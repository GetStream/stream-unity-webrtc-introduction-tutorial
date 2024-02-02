using System;
using System.Collections;
using System.Threading;
using Unity.WebRTC;
using UnityEngine;
using WebRTCTutorial.DTO;

namespace WebRTCTutorial
{
    public class VideoManager : MonoBehaviour
    {
        public event Action<Texture> RemoteVideoReceived;

        public bool CanConnect
            => _peerConnection?.ConnectionState == RTCPeerConnectionState.New ||
               _peerConnection?.ConnectionState == RTCPeerConnectionState.Disconnected;

        public bool IsConnected => _peerConnection?.ConnectionState == RTCPeerConnectionState.Connecting;

        public void SetActiveCamera(WebCamTexture activeWebCamTexture)
        {
            // Remove previous track
            var senders = _peerConnection.GetSenders();
            foreach (var sender in senders)
            {
                _peerConnection.RemoveTrack(sender);
            }

            var videoTrack = new VideoStreamTrack(activeWebCamTexture);
            _peerConnection.AddTrack(videoTrack);
            
            Debug.Log("Sender video track was set");
        }

        public void Connect()
        {
            StartCoroutine(CreateAndSendLocalSdpOffer());
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                return;
            }

            _peerConnection.Close();
            _peerConnection.Dispose();
        }

        // Called by Unity -> https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html
        protected void Awake()
        {
            // FindObjectOfType is used for the demo purpose only. In a real production it's better to avoid it for performance reasons
            _webSocketClient = FindObjectOfType<WebSocketClient>();

            StartCoroutine(WebRTC.Update());

            var config = new RTCConfiguration
            {
                iceServers = new RTCIceServer[]
                {
                    new RTCIceServer
                    {
                        urls = new string[]
                        {
                            // Google Stun server
                            "stun:stun.l.google.com:19302"
                        },
                    }
                },
            };

            _peerConnection = new RTCPeerConnection(ref config);

            // "Negotiation" is the exchange of SDP Offer/Answer. Peers describe what media they want to send and agree on, for example, what codecs to use
            // In this tutorial we exchange the SDP Offer/Answer only once when connecting.
            // But in a real production you'd have to repeat the exchange every time the OnNegotiationNeeded event is triggered
            _peerConnection.OnNegotiationNeeded += OnNegotiationNeeded;

            // Triggered when a new network endpoint is found that could potentially be used to establish the connection
            _peerConnection.OnIceCandidate += OnIceCandidate;

            // Triggered when a new track is received
            _peerConnection.OnTrack += OnTrack;

            // Triggered when a new message is received from the other peer via WebSocket
            _webSocketClient.MessageReceived += OnWebSocketMessageReceived;
        }

        private WebSocketClient _webSocketClient;
        private RTCPeerConnection _peerConnection;

        private void OnTrack(RTCTrackEvent trackEvent)
        {
            Debug.Log("OnTrack received, type: " + trackEvent.Track.Kind);

            if (trackEvent.Track is VideoStreamTrack videoStreamTrack)
            {
                videoStreamTrack.OnVideoReceived += OnVideoReceived;
            }
            else
            {
                Debug.LogError(
                    $"Unhandled track of type: {trackEvent.Track.GetType()}. In this tutorial, we're handling only video tracks.");
            }
        }

        private void OnVideoReceived(Texture texture)
        {
            Debug.Log($"Video received, resolution: {texture.width}x{texture.height}");
            RemoteVideoReceived?.Invoke(texture);
        }

        private void OnNegotiationNeeded()
        {
            Debug.Log("SDP Offer <-> Answer exchange requested by the webRTC client.");
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
            SendIceCandidateToOtherPeer(candidate);
            Debug.Log("Sent Ice Candidate to the other peer THREAD  " + Thread.CurrentThread.ManagedThreadId);
        }

        private void OnWebSocketMessageReceived(string message)
        {
            var dtoWrapper = JsonUtility.FromJson<DTOWrapper>(message);
            switch ((DtoType)dtoWrapper.Type)
            {
                case DtoType.ICE:

                    var iceDto = JsonUtility.FromJson<ICECanddidateDTO>(dtoWrapper.Payload);
                    var ice = new RTCIceCandidate(new RTCIceCandidateInit
                    {
                        candidate = iceDto.Candidate,
                        sdpMid = iceDto.SdpMid,
                        sdpMLineIndex = iceDto.SdpMLineIndex
                    });

                    _peerConnection.AddIceCandidate(ice);
                    Debug.Log($"Received ICE Candidate: {ice.Candidate}");

                    break;
                case DtoType.SDP:

                    var sdpDto = JsonUtility.FromJson<SdpDTO>(dtoWrapper.Payload);
                    var sdp = new RTCSessionDescription
                    {
                        type = (RTCSdpType)sdpDto.Type,
                        sdp = sdpDto.Sdp
                    };

                    Debug.Log($"Received SDP offer of type: {sdp.type} and SDP details: {sdp.sdp}");

                    switch (sdp.type)
                    {
                        case RTCSdpType.Offer:
                            StartCoroutine(OnRemoteSdpOfferReceived(sdp));
                            break;
                        case RTCSdpType.Answer:
                            StartCoroutine(OnRemoteSdpAnswerReceived(sdp));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Unhandled type of SDP message: " + sdp.type);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SendIceCandidateToOtherPeer(RTCIceCandidate iceCandidate)
        {
            var iceDto = new ICECanddidateDTO
            {
                Candidate = iceCandidate.Candidate,
                SdpMid = iceCandidate.SdpMid,
                SdpMLineIndex = iceCandidate.SdpMLineIndex
            };

            SendMessageToOtherPeer(iceDto, DtoType.ICE);
        }

        private void SendSdpToOtherPeer(RTCSessionDescription sdp)
        {
            var sdpDto = new SdpDTO
            {
                Type = (int)sdp.type,
                Sdp = sdp.sdp
            };

            SendMessageToOtherPeer(sdpDto, DtoType.SDP);
        }

        private void SendMessageToOtherPeer<TType>(TType obj, DtoType type)
        {
            try
            {
                var serializedPayload = JsonUtility.ToJson(obj);

                var dtoWrapper = new DTOWrapper
                {
                    Type = (int)type,
                    Payload = serializedPayload
                };

                var serializedDto = JsonUtility.ToJson(dtoWrapper);

                _webSocketClient.SendWebSocketMessage(serializedDto);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private IEnumerator CreateAndSendLocalSdpOffer()
        {
            // 1. Create local SDP offer
            var createOfferOperation = _peerConnection.CreateOffer();
            yield return createOfferOperation;

            if (createOfferOperation.IsError)
            {
                Debug.LogError("Failed to create offer");
                yield break;
            }

            var sdpOffer = createOfferOperation.Desc;

            // 2. Set the offer as a local SDP 
            var setLocalSdpOperation = _peerConnection.SetLocalDescription(ref sdpOffer);
            yield return setLocalSdpOperation;

            if (setLocalSdpOperation.IsError)
            {
                Debug.LogError("Failed to set local description");
                yield break;
            }

            // 3. Send the SDP Offer to the other Peer
            SendSdpToOtherPeer(sdpOffer);
            Debug.Log("Sent Sdp Offer");
        }

        private IEnumerator OnRemoteSdpOfferReceived(RTCSessionDescription remoteSdpOffer)
        {
            Debug.Log("Remote SDP Offer received. Set as local offer and send back the generated answer");

            // 1. Set the received offer as remote description
            var setRemoteSdpOperation = _peerConnection.SetRemoteDescription(ref remoteSdpOffer);
            yield return setRemoteSdpOperation;

            if (setRemoteSdpOperation.IsError)
            {
                Debug.LogError("Failed to set remote description");
                yield break;
            }

            // 2. Generate Answer
            var createAnswerOperation = _peerConnection.CreateAnswer();
            yield return createAnswerOperation;

            if (createAnswerOperation.IsError)
            {
                Debug.LogError("Failed to create answer");
                yield break;
            }

            var sdpAnswer = createAnswerOperation.Desc;

            // 3. Set the generated answer as local description

            var setLocalDspOperation = _peerConnection.SetLocalDescription(ref sdpAnswer);
            yield return setLocalDspOperation;

            if (setLocalDspOperation.IsError)
            {
                Debug.LogError("Failed to set local description");
                yield break;
            }

            // 4. Send the answer to the other Peer
            SendSdpToOtherPeer(sdpAnswer);
            Debug.Log("Sent Sdp Answer");
        }

        private IEnumerator OnRemoteSdpAnswerReceived(RTCSessionDescription remoteSdpAnswer)
        {
            // 1. Set the received answer as remote description
            var setRemoteSdpOperation = _peerConnection.SetRemoteDescription(ref remoteSdpAnswer);
            yield return setRemoteSdpOperation;

            if (setRemoteSdpOperation.IsError)
            {
                Debug.LogError("Failed to set remote description");
            }
        }
    }
}