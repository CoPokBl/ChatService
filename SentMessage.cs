using System.Text.Json.Serialization;

namespace ChatService; 

public class SentMessage {
    
    [JsonPropertyName("CreatorName")]
    public string CreatorName { get; set; } = null!;
    
    [JsonPropertyName("Text")]
    public string Text { get; set; } = null!;
    
    [JsonPropertyName("Signature")]
    public string Signature { get; set; } = null!;
}