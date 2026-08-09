using System.Net;
using Scheder.Tools.Config;
using Telegram.Bot.Types;

namespace Scheder.Tools.Proxy;
using Socks5E_LIB;

public class Proxy {
    
    
    
    public static void SetAuthData(Dictionary<string,object> conData) {
        ProxyManager.SetProxyConnectionSettings(conData);
    }

    public static Dictionary<string,object> Start() {
        return ProxyManager.Start();
    }

    public static Dictionary<string,object> ParseProxyString(string line) {
        return ProxyManager.ParseProxyString(line);
    }

    public static HttpClient? SetAutoProxy() {
        if (!Env.UseProxy) return null;

        
        var conLine = Env.ProxyLine!;
        var conData = ParseProxyString(conLine);
        
        
        ProxyManager.SetProxyConnectionSettings(conData);
        var startData = Start();
        var localProxy = $"socks5://{startData["RUN_ON_IP"]}:{startData["RUN_ON_PORT"]}";
        var (login, pass) = (conData["SERVER_AUTH_LOGIN"].ToString(), conData["SERVER_AUTH_PASS"].ToString());
        var creds = (login != null && pass != null) ? new NetworkCredential(login, pass) : null;
        
        var proxy = new WebProxy(localProxy)
        {
            Credentials = creds
        };
        HttpClient httpClient = new (
            new SocketsHttpHandler { Proxy = proxy, UseProxy = true}
        );

        return httpClient;



    }
}