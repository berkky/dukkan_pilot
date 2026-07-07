using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Models.PublicMenu;

namespace DukkanPilot.Web.Helpers;

public static class PublicOrderDisplayHelper
{
    public static string GetStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Beklemede",
        OrderStatus.Preparing => "Hazırlanıyor",
        OrderStatus.Completed => "Tamamlandı",
        OrderStatus.Cancelled => "İptal",
        _ => status.ToString()
    };

    public static string GetStatusBadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-warning text-dark",
        OrderStatus.Preparing => "bg-info text-dark",
        OrderStatus.Completed => "bg-success",
        OrderStatus.Cancelled => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string GetStatusMessage(OrderStatus status) => status switch
    {
        OrderStatus.Pending =>
            "Siparişiniz işletmeye iletilmek üzere hazırlandı. WhatsApp üzerinden göndermeyi unutmayın.",
        OrderStatus.Preparing => "Siparişiniz hazırlanıyor.",
        OrderStatus.Completed => "Siparişiniz tamamlandı. Afiyet olsun.",
        OrderStatus.Cancelled =>
            "Siparişiniz iptal edildi. Detay için işletmeyle iletişime geçebilirsiniz.",
        _ => "Sipariş durumunuzu bu ekrandan takip edebilirsiniz."
    };

    public static IReadOnlyList<PublicOrderTimelineStepViewModel> GetTimelineSteps(OrderStatus status)
    {
        if (status == OrderStatus.Cancelled)
        {
            return
            [
                new PublicOrderTimelineStepViewModel
                {
                    Key = "received",
                    Label = "Sipariş Alındı",
                    IsActive = true
                },
                new PublicOrderTimelineStepViewModel
                {
                    Key = "cancelled",
                    Label = "İptal Edildi",
                    IsActive = true,
                    IsCurrent = true,
                    IsCancelled = true
                }
            ];
        }

        return
        [
            new PublicOrderTimelineStepViewModel
            {
                Key = "received",
                Label = "Sipariş Alındı",
                IsActive = true,
                IsCurrent = status == OrderStatus.Pending
            },
            new PublicOrderTimelineStepViewModel
            {
                Key = "preparing",
                Label = "Hazırlanıyor",
                IsActive = status is OrderStatus.Preparing or OrderStatus.Completed,
                IsCurrent = status == OrderStatus.Preparing
            },
            new PublicOrderTimelineStepViewModel
            {
                Key = "completed",
                Label = "Tamamlandı",
                IsActive = status == OrderStatus.Completed,
                IsCurrent = status == OrderStatus.Completed
            }
        ];
    }
}
