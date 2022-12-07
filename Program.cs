using GeneralPurposeLib;

namespace ChatService;

public static class Program {
    
    public static int Main(string[] args) {
        Logger.Init(LogLevel.Debug);
        
        Logger.Info("Starting services...");
        
        // Create cancellation token
        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;
        
        Console.CancelKeyPress += (_, eventArgs) => {
            Logger.Info("Stopping services...");
            cts.Cancel();
            eventArgs.Cancel = true;
        };

        Task apiTask = ApiService.Start(token);
        Task liveTask = LiveUpdateService.Run(token);

        Logger.Info("Services started.");

        try {
            Task.WaitAll(apiTask, liveTask);
        }
        catch (AggregateException e) {
            foreach (Exception innerException in e.InnerExceptions) {
                if (innerException is not TaskCanceledException) {
                    Logger.Error("Fatal error: " + innerException);
                }
                else {
                    Logger.Debug("Caught TaskCanceledException");
                }
            }
        }
        Logger.Info("All services stopped.");
        return 0;
    }
    
}