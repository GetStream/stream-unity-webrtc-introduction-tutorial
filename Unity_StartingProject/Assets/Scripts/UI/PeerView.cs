using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace WebRTCTutorial.UI
{
    public class PeerView : MonoBehaviour
    {
#if UNITY_EDITOR
        // Called by Unity https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html
        protected void OnValidate()
        {
            try
            {
                // Validate that all references are connected
                Assert.IsNotNull(_videoRender);
            }
            catch (Exception)
            {
                Debug.LogError($"Some of the references are NULL, please inspect the {nameof(PeerView)} script on this object", this);
            }
        }
#endif
    
        [SerializeField]
        private RawImage _videoRender;
    }
}
