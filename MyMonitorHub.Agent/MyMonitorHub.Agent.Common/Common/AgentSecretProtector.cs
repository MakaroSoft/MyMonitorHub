using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Serilog;

namespace MyMonitorHub.Agent.Common
{
    /// <summary>
    /// DPAPI-based protect/unprotect for agent secrets stored in appsettings.json.
    /// Uses LocalMachine scope so the service (LOCAL SYSTEM) can decrypt at startup.
    /// Entropy is a per-machine random 32-byte secret stored in
    /// %PROGRAMDATA%\MyMonitorHub.Agent\entropy.bin, readable only by SYSTEM
    /// and Administrators. The shared location lets Setup and the Service share
    /// the same entropy regardless of where each .exe lives on disk.
    /// </summary>
    public static class AgentSecretProtector
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(AgentSecretProtector));

        private const string EntropyFileName   = "entropy.bin";
        private const string EntropyFolderName = "MyMonitorHub.Agent";

        private static string EntropyFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                EntropyFolderName);

        private static string EntropyPath =>
            Path.Combine(EntropyFolder, EntropyFileName);

        public static string Protect(string plaintext)
        {
            var data    = Encoding.UTF8.GetBytes(plaintext);
            var entropy = EnsureEntropy();
            var blob    = ProtectedData.Protect(data, entropy, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(blob);
        }

        /// <summary>
        /// Decrypts a DPAPI-protected value previously produced by <see cref="Protect"/>.
        /// Throws <see cref="AgentSecretProtectionException"/> on any failure â€” missing entropy,
        /// wrong machine, tampered blob, etc. Callers MUST NOT fall back to plaintext: a
        /// protected blob that cannot be decrypted is a hard configuration error and the
        /// process should exit so the operator notices and re-runs Setup.
        /// </summary>
        public static string Unprotect(string protectedBase64)
        {
            if (string.IsNullOrEmpty(protectedBase64))
                throw new AgentSecretProtectionException(
                    "Cannot unprotect an empty value.");

            byte[] blob;
            try
            {
                blob = Convert.FromBase64String(protectedBase64);
            }
            catch (FormatException ex)
            {
                throw new AgentSecretProtectionException(
                    "Protected value in appsettings.json is not valid base-64. " +
                    "The file has likely been corrupted or hand-edited; re-run Setup to restore it.",
                    ex);
            }

            var entropy = TryLoadEntropy();
            if (entropy == null)
            {
                throw new AgentSecretProtectionException(
                    $"Entropy file not found at '{EntropyPath}'. " +
                    "Run the agent Setup (elevated) on this machine to regenerate it, " +
                    "then re-copy the protected api-key into appsettings.json.");
            }

            try
            {
                var data = ProtectedData.Unprotect(blob, entropy, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(data);
            }
            catch (CryptographicException ex)
            {
                throw new AgentSecretProtectionException(
                    "Failed to decrypt protected value. The blob in appsettings.json was " +
                    $"encrypted with a different entropy file or on a different machine than the one at '{EntropyPath}'. " +
                    "Re-run the agent Setup on this machine and copy the freshly-generated " +
                    "api-key-protected value into appsettings.json.",
                    ex);
            }
        }

        // â”€â”€ private helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static byte[]? TryLoadEntropy()
        {
            try
            {
                var path = EntropyPath;
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "AgentSecretProtector: could not read entropy file at {0}.", EntropyPath);
            }
            return null;
        }

        private static byte[] EnsureEntropy()
        {
            var existing = TryLoadEntropy();
            if (existing != null) return existing;

            EnsureEntropyFolder();

            var entropy = new byte[32];
            RandomNumberGenerator.Fill(entropy);

            var path = EntropyPath;
            File.WriteAllBytes(path, entropy);
            RestrictEntropyFileAccess(path);

            Logger.Information("AgentSecretProtector: generated per-machine entropy at {0}", path);
            return entropy;
        }

        private static void EnsureEntropyFolder()
        {
            var folder = EntropyFolder;
            var created = !Directory.Exists(folder);
            Directory.CreateDirectory(folder);
            if (created)
            {
                RestrictEntropyFolderAccess(folder);
            }
        }

        private static void RestrictEntropyFolderAccess(string folder)
        {
            try
            {
                var dirInfo  = new DirectoryInfo(folder);
                var security = dirInfo.GetAccessControl();

                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

                var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

                security.AddAccessRule(new FileSystemAccessRule(
                    system,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));
                security.AddAccessRule(new FileSystemAccessRule(
                    admins,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                dirInfo.SetAccessControl(security);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "AgentSecretProtector: could not set ACL on entropy folder â€” " +
                                "please restrict {0} to SYSTEM and Administrators manually.", folder);
            }
        }

        private static void RestrictEntropyFileAccess(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                var security = fileInfo.GetAccessControl();

                // Remove inherited rules; grant only SYSTEM and Administrators
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
                security.AddAccessRule(new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                    FileSystemRights.FullControl,
                    AccessControlType.Allow));
                security.AddAccessRule(new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                    FileSystemRights.FullControl,
                    AccessControlType.Allow));
                fileInfo.SetAccessControl(security);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "AgentSecretProtector: could not set ACL on entropy file â€” " +
                                "please restrict {0} to SYSTEM and Administrators manually.", path);
            }
        }

    }

    /// <summary>
    /// Thrown when a DPAPI-protected secret in appsettings.json cannot be decrypted.
    /// This is a fatal configuration error: the agent must not silently fall back to
    /// plaintext, so callers should let this exception terminate the process.
    /// </summary>
    public class AgentSecretProtectionException : Exception
    {
        public AgentSecretProtectionException(string message)
            : base(message) { }

        public AgentSecretProtectionException(string message, Exception inner)
            : base(message, inner) { }
    }
}
