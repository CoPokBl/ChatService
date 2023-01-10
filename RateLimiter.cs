using System.Net;

namespace ChatService; 

public static class RateLimiter {

    private const int MaxRequestsPerMinute = 60;

    private static readonly Dictionary<IPAddress, List<DateTime>> Requests = new();
    
    public static void NewRequest(IPAddress ip) {
        if (!Requests.ContainsKey(ip)) {
            Requests.Add(ip, new List<DateTime>());
        }
        Requests[ip].Add(DateTime.Now);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ip"></param>
    /// <returns>True if they are being ratelimited otherwise false.</returns>
    public static bool CheckUser(IPAddress ip) {
        if (!Requests.ContainsKey(ip)) {
            return false;
        }
        RemoveOldRequests(ip);
        return Requests[ip].Count >= MaxRequestsPerMinute;
    }
    
    private static void RemoveOldRequests(IPAddress ip) {
        if (!Requests.ContainsKey(ip)) return;
        Requests[ip] = Requests[ip].Where(x => x > DateTime.Now.AddSeconds(-60)).ToList();
    }
    
    public static string GetInfoString(IPAddress ip) {
        if (!Requests.ContainsKey(ip)) return $"0/{MaxRequestsPerMinute}";
        RemoveOldRequests(ip);
        return $"{Requests[ip].Count}/{MaxRequestsPerMinute}";
    }

}