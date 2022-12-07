namespace ChatService; 

public static class MessageHandler {
    
    private static readonly Dictionary<string, List<Message>> Messages = new();
    public static event Action<(string, Message)>? OnNewMessage;

    public static Message[] GetMessages(string chatroom, int amount = 12, int offset = 0) {
        if (!Messages.ContainsKey(chatroom)) {
            Messages.Add(chatroom, new List<Message>());
        }
        return Messages[chatroom].Skip(offset).TakeLast(amount).ToArray();
    }
    
    public static void AddMessage(string chatroom, Message message) {
        if (!Messages.ContainsKey(chatroom)) {
            Messages.Add(chatroom, new List<Message>());
        }
        Messages[chatroom].Add(message);
        OnNewMessage?.Invoke((chatroom, message));
    }

}