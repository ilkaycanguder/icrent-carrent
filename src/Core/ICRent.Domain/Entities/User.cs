using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICRent.Domain.Entities
{
    public sealed class User
    {
        public int Id { get; init; }
        public string UserName { get; init; } = null!;
        public byte[] PasswordHash { get; init; } = null!;
        public byte[] PasswordSalt { get; init; } = null!;
        public string Role { get; init; } = null!;
        public DateTime CreatedAt { get; init; }
    }
}
