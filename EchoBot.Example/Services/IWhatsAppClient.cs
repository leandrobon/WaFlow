namespace EchoBot.Example.Services;

public interface IWhatsAppClient
{
    Task SendTextAsync(string to, string text, CancellationToken ct = default);
}