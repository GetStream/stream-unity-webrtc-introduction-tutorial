namespace WebRTCTutorial.DTO
{
    /// <summary>
    /// DTO (Data Transfer Object) to send/receive ICE Candidate through the network. This DTO maps to <see cref="RTCIceCandidate"/>
    /// </summary>
    [System.Serializable]
    public class ICECanddidateDTO
    {
        public string Candidate;
        public string SdpMid;
        public int? SdpMLineIndex;
    }
}