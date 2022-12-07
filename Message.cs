using System.Text.Json.Serialization;

namespace ChatService; 

public class Message {
    
    [JsonPropertyName("MessageId")]
    public string MessageId { get; set; }
    
    [JsonPropertyName("CreatorName")]
    public string CreatorName { get; set; }
    
    [JsonPropertyName("Text")]
    public string Text { get; set; }
    
    [JsonPropertyName("CreatedAt")]
    public long CreatedAt { get; set; }
    
    [JsonPropertyName("Signature")]
    public string Signature { get; set; }

    public Message(SentMessage msg) {
        CreatorName = msg.CreatorName;
        Text = msg.Text;
        Signature = msg.Signature;
        CreatedAt = DateTime.UtcNow.ToBinary();
        MessageId = Guid.NewGuid().ToString();
    }

    public Message() { }

}