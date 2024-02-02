using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace WebRTCTutorial.UI
{
    public class UIManager : MonoBehaviour
    {
#if UNITY_EDITOR
        // Called by Unity -> https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html
        protected void OnValidate()
        {
            try
            {
                // Validate that all references are connected
                Assert.IsNotNull(_peerViewA);
                Assert.IsNotNull(_peerViewB);
                Assert.IsNotNull(_cameraDropdown);
                Assert.IsNotNull(_connectButton);
                Assert.IsNotNull(_disconnectButton);
            }
            catch (Exception)
            {
                Debug.LogError(
                    $"Some of the references are NULL, please inspect the {nameof(UIManager)} script on this object",
                    this);
            }
        }
#endif

        // Called by Unity -> https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html
        protected void Awake()
        {
            // FindObjectOfType is used for the demo purpose only. In a real production it's better to avoid it for performance reasons
            _videoManager = FindObjectOfType<VideoManager>();

            // Check if there's any camera device available
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError(
                    "No Camera devices available! Please make sure a camera device is detected and accessible by Unity. " +
                    "This demo application will not work without a camera device.");
            }

            // Subscribe to buttons
            _connectButton.onClick.AddListener(OnConnectButtonClicked);
            _disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);

            // Clear default options from the dropdown
            _cameraDropdown.ClearOptions();

            // Populate dropdown with the available camera devices
            foreach (var cameraDevice in WebCamTexture.devices)
            {
                _cameraDropdown.options.Add(new TMP_Dropdown.OptionData(cameraDevice.name));
            }

            // Change the active camera device when new dropdown value is selected
            _cameraDropdown.onValueChanged.AddListener(SetActiveCamera);

            // Subscribe to when video from the other peer is received
            _videoManager.RemoteVideoReceived += OnRemoteVideoReceived;
        }

        // Called by Unity -> https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html
        protected void Start()
        {
            // Enable first camera from the dropdown.
            // We call it in Start to make sure that Awake of all game objects completed and all scripts 
            SetActiveCamera(deviceIndex: 0);
        }

        // Called by Unity -> https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html
        protected void Update()
        {
            // Control buttons being clickable by the connection state
            // In a real production code you may want to have this event driven instead of per frame operation
            _connectButton.interactable = _videoManager.CanConnect;
            _disconnectButton.interactable = _videoManager.IsConnected;
        }

        [SerializeField]
        private PeerView _peerViewA;

        [SerializeField]
        private PeerView _peerViewB;

        [SerializeField]
        private TMP_Dropdown _cameraDropdown;

        [SerializeField]
        private Button _connectButton;

        [SerializeField]
        private Button _disconnectButton;

        private WebCamTexture _activeCamera;

        private VideoManager _videoManager;

        private void SetActiveCamera(int deviceIndex)
        {
            var deviceName = _cameraDropdown.options[deviceIndex].text;

            // Stop previous camera capture
            if (_activeCamera != null && _activeCamera.isPlaying)
            {
                _activeCamera.Stop();
            }

            /* Depending on the platform you're targeting you may need to request permission to access the camera device:
                - IOS or WebGL -> https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application.RequestUserAuthorization.html
                - Android -> https://docs.unity3d.com/Manual/android-RequestingPermissions.html
             */

            // Some platforms (like Android) require 16x16 alignment for the texture size to be sent via WebRTC
            _activeCamera = new WebCamTexture(deviceName, 1024, 768, requestedFPS: 30);

            _activeCamera.Play();

            // starting the camera might fail if the device is not accessible (e.g. used by another application)
            if (!_activeCamera.isPlaying)
            {
                Debug.LogError($"Failed to start the `{deviceName}` camera device.");
                return;
            }

            StartCoroutine(PassActiveCameraToVideoManager());
        }

        /// <summary>
        /// Starting the camera is an asynchronous operation.
        /// If we create the video track before camera is active it may have an invalid resolution.
        /// Therefore, it's best to wait until camera is in fact started before passing it to the video track
        /// </summary>
        private IEnumerator PassActiveCameraToVideoManager()
        {
            var timeElapsed = 0f;
            while (!_activeCamera.didUpdateThisFrame)
            {
                yield return null;

                // infinite loop prevention
                timeElapsed += Time.deltaTime;
                if (timeElapsed > 5f)
                {
                    Debug.LogError("Camera didn't start after 5 seconds. Aborting. The video track is not created.");
                    yield break;
                }
            }
            
            // Set preview of the local peer
            _peerViewA.SetVideoTexture(_activeCamera);

            // Notify Video Manager about new active camera device
            _videoManager.SetActiveCamera(_activeCamera);
        }

        private void OnRemoteVideoReceived(Texture texture)
        {
            _peerViewB.SetVideoTexture(texture);
        }

        private void OnConnectButtonClicked() => _videoManager.Connect();

        private void OnDisconnectButtonClicked() => _videoManager.Disconnect();
    }
}