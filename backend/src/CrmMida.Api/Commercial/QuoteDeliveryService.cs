using System.Net;
using System.Net.Mail;
using System.Net.Http.Json;
using CrmMida.Domain.Commercial;

namespace CrmMida.Api.Commercial;

public sealed class QuoteDeliveryService(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    QuotePdfService pdfService)
{
    public async Task<QuoteDeliveryResult> SendAsync(
        Quote quote,
        string channel,
        string recipient,
        string? message,
        CancellationToken cancellationToken)
    {
        var normalizedChannel = channel.Trim().ToLowerInvariant();
        var normalizedRecipient = recipient.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRecipient))
            return QuoteDeliveryResult.Failed(normalizedChannel, "El destinatario es obligatorio.");

        return normalizedChannel switch
        {
            "email" => await SendEmailAsync(quote, normalizedRecipient, message, cancellationToken),
            "whatsapp" => await SendWhatsAppAsync(quote, normalizedRecipient, message, cancellationToken),
            _ => QuoteDeliveryResult.Failed(normalizedChannel, "Canal no válido. Usa email o whatsapp.")
        };
    }

    private async Task<QuoteDeliveryResult> SendEmailAsync(
        Quote quote,
        string recipient,
        string? message,
        CancellationToken cancellationToken)
    {
        var host = configuration["QuoteDelivery:Smtp:Host"];
        var user = configuration["QuoteDelivery:Smtp:User"];
        var password = configuration["QuoteDelivery:Smtp:Password"];
        var from = configuration["QuoteDelivery:Smtp:From"];
        var port = int.TryParse(configuration["QuoteDelivery:Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        var enableSsl = !bool.TryParse(configuration["QuoteDelivery:Smtp:EnableSsl"], out var parsedSsl) || parsedSsl;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            return QuoteDeliveryResult.NotConfigured("email", "SMTP no está configurado.");

        try
        {
            using var mail = new MailMessage(from, recipient)
            {
                Subject = $"Cotización {quote.Folio} · MIDA",
                Body = BuildMessage(quote, message),
                IsBodyHtml = false
            };

            var pdf = pdfService.Generate(quote);
            mail.Attachments.Add(new Attachment(new MemoryStream(pdf), $"{quote.Folio}.pdf", "application/pdf"));

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = string.IsNullOrWhiteSpace(user)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(user, password)
            };

            await client.SendMailAsync(mail, cancellationToken);
            return QuoteDeliveryResult.Sent("email", "Cotización enviada por correo.");
        }
        catch (Exception exception)
        {
            return QuoteDeliveryResult.Failed("email", exception.Message);
        }
    }

    private async Task<QuoteDeliveryResult> SendWhatsAppAsync(
        Quote quote,
        string recipient,
        string? message,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["QuoteDelivery:Evolution:BaseUrl"];
        var instance = configuration["QuoteDelivery:Evolution:Instance"];
        var apiKey = configuration["QuoteDelivery:Evolution:ApiKey"];
        var publicApiUrl = configuration["QuoteDelivery:PublicApiUrl"]?.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(instance) || string.IsNullOrWhiteSpace(apiKey))
            return QuoteDeliveryResult.NotConfigured("whatsapp", "Evolution API no está configurada.");

        if (string.IsNullOrWhiteSpace(publicApiUrl))
            return QuoteDeliveryResult.NotConfigured("whatsapp", "Configura QuoteDelivery:PublicApiUrl para compartir el PDF.");

        try
        {
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("apikey", apiKey);

            var pdfUrl = $"{publicApiUrl}/api/v1/quotes/{quote.Id}/pdf";
            var text = $"{BuildMessage(quote, message)}\n\nPDF: {pdfUrl}";
            var payload = new { number = NormalizePhone(recipient), text };
            var response = await client.PostAsJsonAsync($"message/sendText/{instance}", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return QuoteDeliveryResult.Failed("whatsapp", $"Evolution API respondió {(int)response.StatusCode}: {responseBody}");
            }

            return QuoteDeliveryResult.Sent("whatsapp", "Cotización enviada por WhatsApp.");
        }
        catch (Exception exception)
        {
            return QuoteDeliveryResult.Failed("whatsapp", exception.Message);
        }
    }

    private static string BuildMessage(Quote quote, string? customMessage)
    {
        var greeting = string.IsNullOrWhiteSpace(customMessage)
            ? "Compartimos la cotización solicitada."
            : customMessage.Trim();

        return $"{greeting}\n\nFolio: {quote.Folio}\nEmpresa: {quote.Company?.TradeName}\nTotal: {quote.Currency} {quote.Total:N2}\nVigencia: {quote.ValidUntilUtc:dd/MM/yyyy}";
    }

    private static string NormalizePhone(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}

public sealed record QuoteDeliveryResult(string Channel, string Status, string Message)
{
    public static QuoteDeliveryResult Sent(string channel, string message) => new(channel, "sent", message);
    public static QuoteDeliveryResult NotConfigured(string channel, string message) => new(channel, "not_configured", message);
    public static QuoteDeliveryResult Failed(string channel, string message) => new(channel, "failed", message);
}
