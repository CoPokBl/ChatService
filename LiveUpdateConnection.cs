using System.Net.Sockets;
using System.Text;
using GeneralPurposeLib;

namespace ChatService; 

public class LiveUpdateConnection {
    private readonly Socket _socket;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<string> _sendMsgQueue;
    private OnlineUser? _user;

    // Info
    private string _username;
    private string _pubKey;
    private string _channel;

    public LiveUpdateConnection(Socket socket, CancellationToken globalCancellationToken) {
        _socket = socket;
        _username = Guid.NewGuid().ToString();
        _pubKey = "";
        _channel = "";
        _cancellationTokenSource = new CancellationTokenSource();
        _sendMsgQueue = new List<string>();

        // Stop the connection if the global token is cancelled
        globalCancellationToken.Register(() => {
            _cancellationTokenSource.Cancel();
        });
    }
    
    public Task HandleConnection() {
        Logger.Debug($"[{_username}] Handling connection");

        _cancellationTokenSource.Token.Register(() => {
            LiveUpdateService.UserDisconnected(_user!);
            try {
                SendMessage(_socket, "DISCONNECT");
            }
            catch (Exception) {
                Logger.Debug($"[{_username}] Error sending disconnect message");
            }
        });
        
        try {
            NetworkStream stream = new(_socket);

            // Start by asking the client for their username
            SendMessage(_socket, "USERNAME");

            // Wait for the client to respond with the username
            _username = ReceiveMessage(stream);

            // Ask for public key
            SendMessage(_socket, "PUBKEY");

            // Wait for the client to respond with the public key
            _pubKey = ReceiveMessage(stream);
            
            // Ask to sign random text
            string textToSign = Guid.NewGuid().ToString();
            SendMessage(_socket, "SIGN " + textToSign);
            
            // Wait for the client to respond with the signature
            string signature = ReceiveMessage(stream);
            
            // Verify the signature
            if (!KeySigning.VerifySignature(_pubKey, signature, textToSign)) {
                Logger.Debug($"[{_username}] Signature verification failed");
                _cancellationTokenSource.Cancel();
                return Task.CompletedTask;
            }

            // Ask for channel
            SendMessage(_socket, "CHANNEL");

            // Wait for the client to respond with the channel
            _channel = ReceiveMessage(stream);

            // Acknowledge the inputs
            SendMessage(_socket, "ACK");

            // Wait for acknowledgement from the client indicating that they are ready to receive messages
            string ack = ReceiveMessage(stream);
            if (ack != "ACK") {
                throw new Exception("Client did not acknowledge message");
            }

            // Start listening for messages
            MessageHandler.OnNewMessage += NewMsg;
            LiveUpdateService.OnUserConnected += UserOnline;
            LiveUpdateService.OnUserDisconnected += UserOffline;

            Logger.Debug($"[{_username}] {_username} connected to {_channel}");
            _user = new OnlineUser {
                Username = _username,
                PublicKey = _pubKey,
                Channel = _channel
            };
            LiveUpdateService.UserConnected(_user);
        }
        catch (Exception e) {
            Logger.Debug($"[{_username}] Socket disconnect: " + e.Message);
            _cancellationTokenSource.Cancel();
        }

        // Wait until the cancellation token is cancelled
        _cancellationTokenSource.Token.WaitHandle.WaitOne();

        Logger.Debug($"[{_username}] Token cancelled, finishing...");
        return Task.CompletedTask;
    }

    // THIS FUNCTION NEEDS TO BE NON BLOCKING
    private void NewMsg((string, Message) data) {
        //Logger.Debug($"[{_username}] New message received, sending to {_username}");
        Thread newMsgAsyncTask = new(NewMsgAsync);
        newMsgAsyncTask.Start(data);
        //Logger.Debug($"[{_username}] New message sent to {_username}");
    }
    
    private void UserOnline(OnlineUser user) {
        if (user == _user) {
            return;
        }
        Thread userOnlineAsyncTask = new(UserOnlineAsync);
        userOnlineAsyncTask.Start(user);
    }
    
    private void UserOffline(OnlineUser user) {
        if (user == _user) {
            return;
        }
        Thread userOfflineAsyncTask = new(UserOfflineAsync);
        userOfflineAsyncTask.Start(user);
    }
    
    private void NewMsgAsync(object? dataObj) {
        
        (string, Message) data = ((string, Message)) dataObj!;
        if (data.Item1 != _channel) {  // If the message is not for this channel, ignore it
            return;
        }
        
        string myId = Guid.NewGuid().ToString();
        _sendMsgQueue.Add(myId);
        
        // Wait until myId is the first element in the queue
        while (_sendMsgQueue[0] != myId) {
            Thread.Sleep(1);
        }

        Logger.Debug($"[{_username}] Sending message to " + _username);

        try {
            NetworkStream stream = new(_socket);

            Task<string> GetClientAck() {
                return Task.Run(() => ReceiveMessage(stream), _cancellationTokenSource.Token);
            }
            
            // Send the message to the client
            SendMessage(_socket, "MSG " + data.Item2.ToJson());
            
            // Wait for acknowledgement
            Task<string> clientAckTask = GetClientAck();
            // Give the client 10 seconds to respond
            if (!clientAckTask.Wait(10_000, _cancellationTokenSource.Token)) {
                throw new Exception("Client did not acknowledge message within 10 seconds");
            }
            
            string ack = clientAckTask.Result;
            if (ack != "ACK") {
                throw new Exception("Client did not acknowledge message");
            }
        }
        catch (Exception e) {
            Logger.Debug($"[{_username}] Socket disconnect: " + e.Message);
            _cancellationTokenSource.Cancel();
        }
        Logger.Debug($"[{_username}] Finished sending message to " + _username);
        _sendMsgQueue.RemoveAt(0);
    }
    
    private void UserOnlineAsync(object? dataObj) {
        
        OnlineUser user = (OnlineUser) dataObj!;

        string myId = Guid.NewGuid().ToString();
        _sendMsgQueue.Add(myId);
        
        // Wait until myId is the first element in the queue
        while (_sendMsgQueue[0] != myId) {
            Thread.Sleep(1);
        }

        Logger.Debug($"[{_username}] Sending online user to " + _username);
        
        
        try {
            NetworkStream stream = new(_socket);

            Task<string> GetClientAck() {
                return Task.Run(() => ReceiveMessage(stream), _cancellationTokenSource.Token);
            }
            
            // Send the message to the client
            SendMessage(_socket, "ONLINE " + user.ToJson());
            
            // Wait for acknowledgement
            Task<string> clientAckTask = GetClientAck();
            // Give the client 10 seconds to respond
            if (!clientAckTask.Wait(10_000, _cancellationTokenSource.Token)) {
                throw new Exception("Client did not acknowledge online user within 10 seconds");
            }
            
            string ack = clientAckTask.Result;
            if (ack != "ACK") {
                throw new Exception("Client did not acknowledge online user");
            }
        }
        catch (Exception e) {
            Logger.Debug($"[{_username}] Socket disconnect: " + e.Message);
            _cancellationTokenSource.Cancel();
        }
        Logger.Debug($"[{_username}] Finished sending offline user to " + _username);
        _sendMsgQueue.RemoveAt(0);
    }
    
    private void UserOfflineAsync(object? dataObj) {
        
        OnlineUser user = (OnlineUser) dataObj!;

        string myId = Guid.NewGuid().ToString();
        _sendMsgQueue.Add(myId);
        
        // Wait until myId is the first element in the queue
        while (_sendMsgQueue[0] != myId) {
            Thread.Sleep(1);
        }

        Logger.Debug($"[{_username}] Sending offline user to " + _username);
        
        
        try {
            NetworkStream stream = new(_socket);

            Task<string> GetClientAck() {
                return Task.Run(() => ReceiveMessage(stream), _cancellationTokenSource.Token);
            }
            
            // Send the message to the client
            SendMessage(_socket, "OFFLINE " + user.ToJson());
            
            // Wait for acknowledgement
            Task<string> clientAckTask = GetClientAck();
            // Give the client 10 seconds to respond
            if (!clientAckTask.Wait(10_000, _cancellationTokenSource.Token)) {
                throw new Exception("Client did not acknowledge offline user within 10 seconds");
            }
            
            string ack = clientAckTask.Result;
            if (ack != "ACK") {
                throw new Exception("Client did not acknowledge offline user");
            }
        }
        catch (Exception e) {
            Logger.Debug($"[{_username}] Socket disconnect: " + e.Message);
            _cancellationTokenSource.Cancel();
        }
        Logger.Debug($"[{_username}] Finished sending offline user to " + _username);
        _sendMsgQueue.RemoveAt(0);
    }

    private static string ReceiveMessage(Stream stream) {
        // Read until we get a newline
        StringBuilder cmdBuilder = new();
        while (true) {
            int b = stream.ReadByte();
            if (b == -1) {
                break;
            }
            if (b == '\n') {
                break;
            }
            cmdBuilder.Append((char)b);
        }
        // unescape the newline
        return cmdBuilder.ToString().Replace("\\n", "\n");
    }
    
    private static void SendMessage(Socket socket, string data) {
        // Escape the newline character
        data = data.Replace("\n", "\\n");
        // Send the data
        socket.Send(Encoding.UTF8.GetBytes(data + "\n"));
    }
    
}