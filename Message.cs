using System.Text.Json.Serialization;

namespace ChatService; 

public class Message {
    
    [JsonPropertyName("MessageId")]
    public string MessageId { get; set; } = null!;

    [JsonPropertyName("CreatorName")]
    public string CreatorName { get; set; } = null!;

    [JsonPropertyName("Text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("CreatedAt")]
    public long CreatedAt { get; set; }
    
    [JsonPropertyName("Signature")]
    public string Signature { get; set; } = null!;

    public Message(SentMessage msg) {
        CreatorName = msg.CreatorName;
        Text = msg.Text;
        Signature = msg.Signature;
        CreatedAt = DateTime.UtcNow.ToBinary();
        MessageId = Guid.NewGuid().ToString();
    }

    public Message() { }

}