using System;

namespace MyMonitorHub.Domain.Entity
{
    public class DeviceRefreshToken
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public int AccountId { get; set; }

        /// <summary>SHA-256 hex digest of the raw refresh token value — never stored in plaintext.</summary>
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>All tokens in one rotation chain share a FamilyId. If a used token is presented again,
        /// the entire family is revoked to detect theft.</summary>
        public Guid FamilyId { get; set; }

        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Revoked { get; set; }

        public virtual Device? Device { get; set; }
    }
}
