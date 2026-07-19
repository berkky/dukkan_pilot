using System.Net;
using DukkanPilot.Mobile.Core.Contracts;

namespace DukkanPilot.Mobile.Core.Api;

public sealed class MobileApiException : Exception
{
    public MobileApiException(
        string code,
        string userMessage,
        HttpStatusCode? statusCode = null,
        string? traceId = null,
        IReadOnlyDictionary<string, string[]>? validationErrors = null,
        IReadOnlyList<MobileBusinessOption>? businesses = null,
        Exception? innerException = null)
        : base(userMessage, innerException)
    {
        Code = code;
        UserMessage = userMessage;
        StatusCode = statusCode;
        TraceId = traceId;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>();
        Businesses = businesses ?? [];
    }

    public string Code { get; }
    public string UserMessage { get; }
    public HttpStatusCode? StatusCode { get; }
    public string? TraceId { get; }
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
    public IReadOnlyList<MobileBusinessOption> Businesses { get; }
}

public static class MobileErrorMessages
{
    public static string ForCode(string? code, HttpStatusCode? statusCode = null)
    {
        return code switch
        {
            "invalid_credentials" => "E-posta veya şifre hatalı.",
            "business_selection_required" => "Devam etmek için bir işletme seçin.",
            "invalid_business" => "Seçilen işletme bu hesap için kullanılamıyor.",
            "account_inactive" => "Bu hesap mobil uygulamayı kullanamıyor.",
            "business_inactive" => "Bu işletme şu anda aktif değil.",
            "rate_limit_exceeded" => "Çok fazla deneme yapıldı. Bir süre sonra tekrar deneyin.",
            "unauthorized" or "invalid_refresh_token" or "refresh_token_expired" or
                "refresh_token_reused" => "Oturumunuz sona erdi. Lütfen yeniden giriş yapın.",
            "forbidden" => "Bu işlem için yetkiniz bulunmuyor.",
            "resource_not_found" => "İstenen kayıt bulunamadı veya artık erişilemiyor.",
            "invalid_order_status" => "Bu sipariş durum değişikliği yapılamıyor.",
            "validation_failed" => "Lütfen girdiğiniz bilgileri kontrol edin.",
            "timeout" => "Sunucu yanıt vermedi. Lütfen tekrar deneyin.",
            "connection_error" => "Sunucuya ulaşılamıyor. İnternet bağlantınızı kontrol edin.",
            "offline" => "İnternet bağlantısı yok. İşlem gönderilmedi.",
            "configuration_error" => "Sunucu bağlantı ayarı geçersiz. Lütfen yöneticinizle iletişime geçin.",
            _ when statusCode == HttpStatusCode.Forbidden => "Bu işlem için yetkiniz bulunmuyor.",
            _ when statusCode == HttpStatusCode.Unauthorized => "Oturumunuz sona erdi. Lütfen yeniden giriş yapın.",
            _ => "İşlem tamamlanamadı. Lütfen tekrar deneyin."
        };
    }
}
