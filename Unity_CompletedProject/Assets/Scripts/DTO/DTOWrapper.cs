namespace WebRTCTutorial.DTO
{
    /// <summary>
    /// A DTO wrapper to simplify the deserialization. When deserializing an object via FromJson (https://docs.unity3d.com/ScriptReference/JsonUtility.FromJson.html) we need to provide a type.
    /// This object will wrap a serialized object and keep it in the <see cref="Payload"/> and provide the <see cref="Type"/> indicating what type should be the payload deserialized into
    /// </summary>
    [System.Serializable]
    public class DTOWrapper
    {
        public int Type;
        public string Payload;
    }
}