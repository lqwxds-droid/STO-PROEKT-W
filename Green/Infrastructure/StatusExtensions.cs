using Green.Models;

namespace Green.Infrastructure
{
    public static class StatusExtensions
    {
        public static string GetRussianName(this RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Planned => "Запланирован",
                RequestStatus.InProgress => "В работе",
                RequestStatus.Completed => "Выполнен",
                RequestStatus.Cancelled => "Отменён",
                RequestStatus.AwaitingApproval => "Ожидает подтверждения",
                _ => "Неизвестно"
            };
        }

        public static string GetRussianNameWithEmoji(this RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Planned => "📋 Запланирован",
                RequestStatus.InProgress => "🔧 В работе",
                RequestStatus.Completed => "✅ Выполнен",
                RequestStatus.Cancelled => "❌ Отменён",
                RequestStatus.AwaitingApproval => "⏳ Ожидает подтверждения",
                _ => "❓ Неизвестно"
            };
        }

        public static string GetBadgeClass(this RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Planned => "bg-warning text-dark",
                RequestStatus.InProgress => "bg-info text-dark",
                RequestStatus.Completed => "bg-success text-light",
                RequestStatus.Cancelled => "bg-danger text-light",
                RequestStatus.AwaitingApproval => "bg-warning text-dark",
                _ => "bg-secondary text-light"
            };
        }
    }
}
