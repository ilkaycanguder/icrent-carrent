using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICRent.Domain.Entities
{
    public sealed class AuditLog
    {
        public long Id { get; init; }
        public int UserId { get; init; }
        public string Action { get; init; } = null!;
        public string Entity { get; init; } = null!;
        public int EntityId { get; init; }
        public string? Details { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? UserName { get; set; }
    }
}
