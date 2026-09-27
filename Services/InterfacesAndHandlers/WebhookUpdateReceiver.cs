namespace Scheder.Services.InterfacesAndHandlers;

using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using static Tools.Logger;


public class WebhookUpdateReceiver
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUpdateHandler _updateHandler;
    private readonly string? _secretToken;

    /// <param name="botClient">Клиент бота (тот же, что используется для polling)</param>
    /// <param name="updateHandler">Существующий UpdateHandler (передавать как IUpdateHandler)</param>
    /// <param name="secretToken">
    /// Значение secret_token, которое ты передашь в SetWebhookAsync.
    /// Если задано — будет сверяться с заголовком X-Telegram-Bot-Api-Secret-Token,
    /// чтобы никто посторонний не мог слать fake-апдейты на твой endpoint.
    /// </param>
    public WebhookUpdateReceiver(ITelegramBotClient botClient, IUpdateHandler updateHandler, string? secretToken = null)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
        _secretToken = secretToken;
    }


    public async Task<bool> HandleAsync(string requestBody, string? receivedSecretToken, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_secretToken) && receivedSecretToken != _secretToken)
        {
            Log.Warning("[Webhook] Отклонён запрос с неверным secret token");
            return false;
        }

        Update? update;
        try
        {
            update = JsonSerializer.Deserialize<Update>(requestBody);
        }
        catch (Exception ex)
        {
            Log.Error("[Webhook] Не удалось распарсить Update: {er}", ex);
            return false;
        }

        if (update == null)
            return false;
        
        await _updateHandler.HandleUpdateAsync(_botClient, update, cancellationToken);
        return true;
    }
}