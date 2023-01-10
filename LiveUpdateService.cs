using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using GeneralPurposeLib;

namespace ChatService; 

public static class LiveUpdateService {

    private static readonly List<OnlineUser> OnlineUsers = new();
    public static event Action<OnlineUser>? OnUserConnected;
    public static event Action<OnlineUser>? OnUserDisconnected;
    public static List<OnlineUser> GetOnlineUsers(string channel) => OnlineUsers.Where(u => u.Channel == channel).ToList();

    public static void UserConnected(OnlineUser u) {
        OnlineUsers.Add(u);
        OnUserConnected?.Invoke(u);
    }
    
    public static void UserDisconnected(OnlineUser u) {
        OnlineUsers.Remove(u);
        OnUserDisconnected?.Invoke(u);
    }

    public static async Task Run(CancellationToken cancellationToken) {
        TcpListener listener = new(IPAddress.Any, 9435);
        listener.Start();

        while (true) {
            Logger.Debug("[Live Update] Waiting for connection...");
            Socket socket = await listener.AcceptSocketAsync(cancellationToken);
            Logger.Debug("[Live Update] Connection accepted.");
            
            async void Start() => await new LiveUpdateConnection(socket, cancellationToken).HandleConnection();
            Thread thread = new(Start);
            thread.Start();
            Logger.Debug("[Live Update] Connection handle begun.");
        }
        
    }

}