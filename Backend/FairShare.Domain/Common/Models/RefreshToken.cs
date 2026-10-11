using System;
using System.Collections.Generic;

namespace FairShare.Domain.Common.Models
{
    public class RefreshToken
    {
        public Guid id { get; set; }
        public required string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }

        public RefreshToken() { }

        public RefreshToken(string refreshToken, DateTime expiresAt, bool isRevoced)
        {
            Token = refreshToken;
            ExpiresAt = expiresAt;
            IsRevoked = isRevoced;
        }
    }  
}
