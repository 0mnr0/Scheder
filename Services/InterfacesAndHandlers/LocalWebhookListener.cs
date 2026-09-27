namespace Scheder.Services.InterfacesAndHandlers;

using System.Net;
using System.Text;
using static Tools.Logger;



public class LocalWebhookListener
{
    private readonly HttpListener _listener = new();
    private readonly WebhookUpdateReceiver _receiver;
    private readonly string _path;
    private CancellationTokenSource? _cts;

    /// <param name="receiver">Уже существующий WebhookUpdateReceiver</param>
    /// <param name="port">Порт, на котором слушать (должен совпадать с тем, куда Flask шлёт запросы)</param>
    /// <param name="path">Путь эндпоинта, например "/bot/webhook/"</param>
    public LocalWebhookListener(WebhookUpdateReceiver receiver, int port = 54890, string path = "/bot/webhook/")
    {
        _receiver = receiver;
        _path = path.EndsWith('/') ? path : path + "/";
        _listener.Prefixes.Add($"http://+:{port}{_path}");
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener.Start();
        Log.Information("[Webhook] Локальный listener запущен на {Prefix}", _listener.Prefixes.First());
        _ = Task.Run(() => LoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener.Stop();
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                break; // остановка через Stop()
            }

            _ = Task.Run(() => HandleContextAsync(context, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                context.Response.Close();
                return;
            }

            string body;
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync(cancellationToken);
            }

            var secret = context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"];

            var ok = await _receiver.HandleAsync(body, secret, cancellationToken);

            context.Response.StatusCode = ok ? 200 : 400;
            context.Response.Close();
        }
        catch (Exception ex)
        {
            Log.Error("[Webhook] Ошибка обработки локального запроса: {er}", ex);
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch { /* соединение уже могло быть закрыто клиентом */ }
        }
    }
}