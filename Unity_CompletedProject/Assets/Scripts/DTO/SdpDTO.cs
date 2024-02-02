using Unity.WebRTC;

namespace WebRTCTutorial.DTO
{
    /// <summary>
    /// DTO (Data Transfer Object) to send/receive SDP Offer or Answer through the network. This DTO maps to <see cref="RTCSessionDescription"/>
    /// </summary>
    [System.Serializable]
    public class SdpDTO
    {
        public int Type;
        public string Sdp;
    }
}