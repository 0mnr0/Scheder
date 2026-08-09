using System.Net;
using Scheder.Tools.Config;
using Telegram.Bot.Types;

namespace Scheder.Tools.Proxy;
using Socks5E_LIB;

public class Proxy {
    
    public static void SetAuthData(Dictionary<string,object> conData) {
        ProxyManager.SetProxyConnectionSettings(conData);
    }

    private static Dictionary<string,object> Start() {
        return ProxyManager.Start();
    }

    private static Dictionary<string,object> ParseProxyString(string line) {
        return ProxyManager.ParseProxyString(line);
    }

    public static HttpClient? SetAutoProxy() {
        if (!Env.UseProxy) return null;

        
        var conLine = Env.ProxyLine!;
        if (string.IsNullOrEmpty(conLine)) {
            Logger.Log.Error("UseProxy set to true but \"ProxyLine\" is null or empty! Proxy is not used!");
            return null;
        }
        
        var conData = ParseProxyString(conLine);
        var scheme = conData["scheme"].ToString()!;
        
        ProxyManager.SetProxyConnectionSettings(conData);
        var startData = Start();
        var (login, pass) = (conData["SERVER_AUTH_LOGIN"].ToString(), conData["SERVER_AUTH_PASS"].ToString());
        var creds = (login != null && pass != null) ? new NetworkCredential(login, pass) : null;
        

        if (scheme.Equals("socks5e", StringComparison.CurrentCultureIgnoreCase)) {
            return RunSocks5E(creds, startData);
        }
        
        if (scheme.Equals("socks5", StringComparison.CurrentCultureIgnoreCase) || scheme.Equals("socks4", StringComparison.CurrentCultureIgnoreCase) || scheme.Equals("socks4a", StringComparison.CurrentCultureIgnoreCase)) {
            return RunSocks(creds, startData, scheme);
        }
        
        if (scheme.Equals("http", StringComparison.CurrentCultureIgnoreCase) || scheme.Equals("https", StringComparison.CurrentCultureIgnoreCase)) {
            return RunHttp(creds, startData);
        }

        return null;
    }


    private static HttpClient RunSocks5E(NetworkCredential? creds, Dictionary<string,object> startData) {
        var forceLine = $"socks5://{startData["RUN_ON_IP"]}:{startData["RUN_ON_PORT"]}";
        return RunSocks(creds, startData, "socks5", forceLine);
    }

    private static HttpClient RunSocks(NetworkCredential? creds, Dictionary<string,object> startData, string scheme, string? forceProxyLine = null) {
        var localProxy = $"{scheme}://{startData["SERVER_IP"]}:{startData["SERVER_PORT"]}";
        if (forceProxyLine != null) {localProxy = forceProxyLine;}
        
        var proxy = new WebProxy(localProxy)
        {
            Credentials = creds
        };
        HttpClient httpClient = new (
            new SocketsHttpHandler { Proxy = proxy, UseProxy = true }
        );

        return httpClient;
    }

    private static HttpClient RunHttp(NetworkCredential? creds, Dictionary<string,object> startData) {
        var localProxy = $"http://{startData["SERVER_IP"]}";
        
        WebProxy  proxy = new(Host: localProxy, Port: int.Parse(startData["SERVER_PORT"]+""))
        {
            Credentials = creds
        };
        HttpClient httpClient = new (
            new SocketsHttpHandler { Proxy = proxy, UseProxy = true }
        );

        return httpClient;
    }
}