using DAL.Entities.Common;
using DAL.Entities.System;

namespace WebAdminMVC.Models.Notifications
{
    public class AdminNotificationViewModel
    {

        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public int TotalReceivers { get; set; }
        public int TotalRead { get; set; }

        public List<Notification> Items { get; set; } = new();

        public SourceType SourceType { get; set; }
    }
}
