using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICRent.Persistence.Repositories.Audits
{
    public sealed record WorkLogAuditDto(
        long Id,
        string User,
        string Action,   // Insert / Update / Delete / Merge
        int EntityId,    // WorkLogs satır Id (veya VehicleId)
        string? Details, // JSON ya da açıklama
        DateTime CreatedAt
    );
}
