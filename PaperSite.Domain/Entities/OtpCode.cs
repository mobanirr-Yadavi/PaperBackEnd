using PaperSite.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PaperSite.Domain.Entities
{
    public class OtpCode:BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string CodeHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
