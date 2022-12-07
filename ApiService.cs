using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;
using GeneralPurposeLib;

namespace ChatService; 

public static class ApiService {
    
    private static readonly HttpListener Listener = new();
    private static bool _run = true;
    
    public static async Task Start(CancellationToken cancellationToken) {
        Listener.Prefixes.Add("http://*:8080/");
        Listener.Start();
        Logger.Info("Started API service");

        cancellationToken.Register(() => _run = false);  // Stop the listener when the token is cancelled

        while (_run) {
            Task<HttpListenerContext> ctxTask = Listener.GetContextAsync();
            await ctxTask.WaitAsync(cancellationToken);  // Wait for a request or cancellation
            HttpListenerContext ctx = ctxTask.Result;
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;
            string responseString = "{ \"Error\":\"No Server Output\" }";
            int statusCode = 200;

            try {
                Logger.Debug("AbsolutePath: " + request.Url!.AbsolutePath);
                string[] path = request.Url!.AbsolutePath.Remove(0, 1).Split('/');
                if (path.Length == 0) {
                    path = new[] {"/"};
                }
                Logger.Debug("Path[0]: " + path[0]);
                switch (path[0].ToLower()) {

                    default:
                        throw new NotFoundException("That endpoint does not exist");
                    
                    case "/":
                        responseString = "<h1>Chat API Service</h1>";
                        break;

                    case "online": {
                        if (path.Length != 1) {
                            throw new BadRequestException("No channel specified (Ex. /channel/SomeRandomChannel)");
                        }
                        if (request.HttpMethod.ToUpper() != "GET") {
                            throw new BadRequestException("Only GET requests are allowed");
                        }
                        responseString = LiveUpdateService.GetOnlineUsers().ToJson();
                        break;
                    }

                    case "channel": {
                        if (path.Length != 2) {
                            throw new BadRequestException("No channel specified (Ex. /channel/SomeRandomChannel)");
                        }

                        switch (request.HttpMethod.ToUpper()) {

                            // Getting messages
                            case "GET": {
                                string channel = path[1];
                                int amount = int.Parse(request.QueryString["amount"] ?? "12");
                                int offset = int.Parse(request.QueryString["offset"] ?? "0");
                                responseString = MessageHandler.GetMessages(channel, amount, offset).ToJson();
                                break;
                            }

                            // Sending a message
                            case "POST": {
                                string channel = path[1];
                                string messageText = await new StreamReader(request.InputStream).ReadToEndAsync();
                                Logger.Debug(messageText);
                                Message message = new(messageText.FromJson<SentMessage>() ??
                                                      throw new BadRequestException("Invalid message"));
                                MessageHandler.AddMessage(channel, message);
                                responseString = message.ToJson();
                                break;
                            }

                            default: {
                                responseString = new {
                                    Error = "hi"
                                }.ToJson();
                                break;
                            }

                        }

                        break;
                    }

                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                await output.WriteAsync(buffer, cancellationToken);
                output.Close();
            }
            catch (BadRequestException e) {
                statusCode = 400;
                responseString = new {
                    Error = e.Message
                }.ToJson();
            }
            catch (NotFoundException e) {
                statusCode = 404;
                responseString = new {
                    Error = e.Message
                }.ToJson();
            }
            catch (Exception e) {
                Logger.Error(e);
            }
            
            // Write the response info
            byte[] data = Encoding.UTF8.GetBytes(responseString);
            try {
                response.ContentType = "application/json";
                response.ContentEncoding = Encoding.UTF8;
                response.ContentLength64 = data.LongLength;
                response.StatusCode = statusCode;
                    
                // Write out to the response stream (asynchronously), then close it
                await response.OutputStream.WriteAsync(data, cancellationToken);
            } catch (ObjectDisposedException) { 
                Logger.Debug("Object disposed crash occured");
                /* Catching is the only reliable way to check if an object is disposed */ 
            }

            
            response.Close();
            
        }
        
        Listener.Stop();
    }

}