using System;
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
    }
}