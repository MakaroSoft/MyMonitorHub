using System.Collections.Generic;

namespace MyMonitorHub.Domain.Interface
{
    public interface IMonitorAuthService
    {
        bool ValidateUser(string email, string password);

        /// <summary>
        /// Returns the canonical email for the given login value.
        /// Returns null if the user is not found.
        /// </summary>
        string? GetCanonicalEmail(string login);

        /// <summary>
        /// Creates a pending registration record and returns a verification token.
        /// Returns (null, errorMessage) if the email is already taken.
        /// </summary>
        (string? Token, string? Error) StartRegistration(string email, string password, string firstName, string lastName);

        /// <summary>
        /// Validates the token from the verification email, creates the user account,
        /// and deletes the pending registration.
        /// Returns (email, null) on success or (null, errorMessage) on failure.
        /// </summary>
        (string? Email, string? Error) ConfirmRegistration(string token);

        /// <summary>
        /// If the email matches a single user account, creates a single-use password
        /// reset token and returns it. Returns null in every other case (no account,
        /// multiple accounts, etc.) so callers can show a generic response and avoid
        /// account enumeration.
        /// </summary>
        string? StartPasswordReset(string email);

        /// <summary>
        /// Validates the password reset token and, if valid, sets the user's password
        /// to <paramref name="newPassword"/> and consumes the token.
        /// Returns (email, null) on success or (null, errorMessage) on failure.
        /// </summary>
        (string? Email, string? Error) ResetPassword(string token, string newPassword);

        /// <summary>
        /// Generates 8 one-time recovery codes for the user and stores their SHA-256 hashes.
        /// Any previously generated codes for the user are deleted.
        /// Returns the plain-text codes to be shown to the user exactly once.
        /// </summary>
        IList<string> GenerateRecoveryCodes(int userId);

        /// <summary>
        /// Validates and consumes a single recovery code for the given email.
        /// Returns true if the code was valid and unused, false otherwise.
        /// The code is normalised (trimmed, uppercased, hyphens removed) before hashing.
        /// </summary>
        bool RedeemRecoveryCode(string email, string code);

        /// <summary>
        /// Disables 2FA for the specified user by clearing TwoFactorEnabled,
        /// TwoFactorSecret, and all stored recovery codes.
        /// </summary>
        void Reset2Fa(int userId);
    }
}
