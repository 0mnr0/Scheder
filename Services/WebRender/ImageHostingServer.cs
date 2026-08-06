namespace Scheder.Services.WebRender;

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Scheder.Tools.Logger;

public class ImageHostingServer
{
    private static readonly HttpClient Client = new();

    private class StoredImage
    {
        public byte[]? Data;
        public Timer? ExpirationTimer;
    }

    private static readonly ConcurrentDictionary<string, StoredImage> Images = new();
    private static readonly HttpListener Listener = new();
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(3);
    private static CancellationTokenSource? _cts;
    private static string? _machineIp;
    private const string LocalAddress = "*";
    private const int LocalPort = 4433;

    public static async Task Start()
    {
        Log.Information("[ImageServer] Starting ImageHostingServer: {S}", $"http://{LocalAddress}:{LocalPort}/");
        Listener.Prefixes.Add($"http://{LocalAddress}:{LocalPort}/");
        _cts = new CancellationTokenSource();
        Listener.Start();
        _machineIp = await Client.GetStringAsync("https://api.ipify.org");
        Log.Information("[ImageServer] running! (Fetched IP: {MachineIp})", _machineIp);
        _ = Task.Run(() => ListenLoop(_cts.Token));
    }

    public static void Stop()
    {
        _cts?.Cancel();
        Listener.Stop();

        foreach (var kv in Images)
            kv.Value.ExpirationTimer.Dispose();
        Images.Clear();
    }


    public static string AddImage(byte[] image)
    {
        var id = Guid.NewGuid().ToString("N");

        var stored = new StoredImage
        {
            Data = image,
            ExpirationTimer = new Timer(_ =>
            {
                Log.Verbose("[ImageServer] request for id={Id}, known ids: {Join}", id, string.Join(",", Images.Keys));
                if (Images.TryRemove(id, out var removed))
                {
                    removed.ExpirationTimer.Dispose();
                }
            }, null, Lifetime, Timeout.InfiniteTimeSpan)
        };


        Images[id] = stored;
        return $"http://{_machineIp}:{LocalPort}/blocks/{id}";
    }

    private static async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await Listener.GetContextAsync();
            }
            catch (Exception)
            {
                if (token.IsCancellationRequested) break;
                continue;
            }

            _ = Task.Run(() => HandleRequest(ctx), token);
        }
    }

    private static void HandleRequest(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url!.AbsolutePath;
            const string prefix = "/blocks/";

            if (path.StartsWith(prefix))
            {
                var id = path[prefix.Length..].Trim('/');

                Log.Verbose("[ImageServer] request for id={Id}, known ids: {Join}", id, string.Join(",", Images.Keys));
                if (Images.TryGetValue(id, out var stored))
                {
                    ctx.Response.ContentType = "image/png";
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentLength64 = stored.Data.Length;
                    ctx.Response.OutputStream.Write(stored.Data, 0, stored.Data.Length);
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                }
            }
            else
            {
                ctx.Response.StatusCode = 404;
            }
        }
        catch
        {
            try { ctx.Response.StatusCode = 500; }
            catch
            {
                // ignored
            }
        }
        finally
        {
            ctx.Response.OutputStream.Close();
        }
    }
}