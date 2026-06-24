using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MyMonitorHub.Agent.Common
{
    public class MonitorProfile
    {
        private static MonitorProfile _current;
        private static readonly object LockObject = new object();
        private static AgentTokenManager _tokenManager;

        private static FileSystemWatcher _watcher;
        private static readonly ILogger Logger = Log.ForContext<MonitorProfile>();

        /// <summary>Singleton token manager â€” valid for the lifetime of the process.</summary>
        public static AgentTokenManager TokenManager
        {
            get
            {
                lock (LockObject)
                {
                    return _tokenManager ?? (_tokenManager = new AgentTokenManager());
                }
            }
        }

        private JObject _config;

        public const string MonitorFileName = "appsettings.json";
        public const string TokenFileName = "refresh-token.dat";

        /// <summary>
        /// Returns the full path to appsettings.json.
        /// When a debugger is attached (i.e. running from Visual Studio), the
        /// MYMONITORHUB_AGENT_CONFIG_DIR environment variable is honoured so that
        /// developers can point both the agent and Setup at a shared config folder
        /// (e.g. C:\Develop\configs\MyMonitorHub.Agent) outside the repository.
        /// In production the variable is ignored and AppContext.BaseDirectory is always used.
        /// </summary>
        public static string MonitorFilePath
        {
            get
            {
                var configDir = Debugger.IsAttached
                    ? Environment.GetEnvironmentVariable("MYMONITORHUB_AGENT_CONFIG_DIR")
                    : null;
                var dir = !string.IsNullOrWhiteSpace(configDir)
                    ? configDir
                    : AppContext.BaseDirectory;
                return Path.Combine(dir, MonitorFileName);
            }
        }

        public static string TokenFilePath
        {
            get { return Path.Combine(Path.GetDirectoryName(MonitorFilePath)!, TokenFileName); }
        }

        public static MonitorProfile Current 
        {
            get
            {
                lock (LockObject)
                {
                    if (_watcher == null)
                    {
                        _watcher = new FileSystemWatcher
                        {
                            Path = AppContext.BaseDirectory,
                            NotifyFilter = NotifyFilters.LastWrite,
                            Filter = MonitorFileName
                        };
                        _watcher.Changed += OnChanged;
                        _watcher.EnableRaisingEvents = true;
                    }
                    return _current ?? (_current = new MonitorProfile());
                }
            }
        }

        public bool Changed { get; private set; }

        private int _accountId;
        private int _deviceId;
        private string _apiKey;
        private bool _apiKeyDirty;
        private string _refreshToken;
        private bool _refreshTokenDirty;

        public ApiKeyState ApiKeyLoadState { get; private set; }
        private string _updateTime;
        private int _commandPort;
        private int _reconnectAttemptMinutes;
        private string _accountName;
        private string _groupName;
        private string _deviceName;
        private string _baseAddress;

        public event EventHandler AccountIdChanged;
        public event EventHandler DeviceIdChanged;
        public event EventHandler ApiKeyChanged;
        public event EventHandler RefreshTokenChanged;
        public event EventHandler UpdateTimeChanged;
        public event EventHandler CommandPortChanged;
        public event EventHandler AccountNameChanged;
        public event EventHandler GroupNameChanged;
        public event EventHandler DeviceNameChanged;
        public event EventHandler ServerOverrideChanged;


        public int AccountId
        {
            get { return _accountId; }
            set
            {
                if (_accountId != value)
                {
                    _accountId = value;
                    Raise(AccountIdChanged, this);
                }
            }
        }

        public int DeviceId
        {
            get { return _deviceId; }
            set
            {
                if (_deviceId != value)
                {
                    _deviceId = value;
                    Raise(DeviceIdChanged, this);
                }
            }
        }

        public string ApiKey
        {
            get { return _apiKey; }
            set
            {
                if (_apiKey != value)
                {
                    _apiKey = value;
                    _apiKeyDirty = true;
                    Raise(ApiKeyChanged, this);
                }
            }
        }

        public string RefreshToken
        {
            get { return _refreshToken; }
            set
            {
                if (_refreshToken != value)
                {
                    _refreshToken = value;
                    _refreshTokenDirty = true;
                    Raise(RefreshTokenChanged, this);
                }
            }
        }

        public string UpdateTime
        {
            get { return _updateTime; }
            set
            {
                if (_updateTime != value)
                {
                    _updateTime = value;
                    Raise(UpdateTimeChanged, this);
                }
            }
        }

        public int CommandPort
        {
            get { return _commandPort; }
            set
            {
                if (_commandPort != value)
                {
                    _commandPort = value;
                    Raise(CommandPortChanged, this);
                }
            }
        }

        /// <summary>
        /// Minutes without a successful event send before the process kills itself to force a restart.
        /// Set to 0 to disable the kill entirely.
        /// </summary>
        public int ReConnectAttemptMinutes
        {
            get { return _reconnectAttemptMinutes; }
        }

        public string AccountName
        {
            get { return _accountName; }
            set
            {
                if (_accountName != value)
                {
                    _accountName = value;
                    Raise(AccountNameChanged, this);
                }
            }
        }

        public string GroupName
        {
            get { return _groupName; }
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
                    Raise(GroupNameChanged, this);
                }
            }
        }

        public string DeviceName
        {
            get { return _deviceName; }
            set
            {
                if (_deviceName != value)
                {
                    _deviceName = value;
                    Raise(DeviceNameChanged, this);
                }
            }
        }

        public string BaseAddress
        {
            get
            {
                return $"https://{_baseAddress}/";
            }
        }

        public string WebSocketAddress
        {
            get
            {
                return $"wss://{_baseAddress}";
            }
        }


        public MonitorProfile(string configJson)
        {
            _config = JObject.Parse(configJson);
            Load();
        }

        private MonitorProfile()
        {
            _config = JObject.Parse(File.ReadAllText(MonitorFilePath));
            Load();
        }

        public string[] ServiceNames
        {
            get
            {
                var servicesArray = _config["server"]?["services"] as JArray;
                if (servicesArray == null) return Array.Empty<string>();
                var names = new string[servicesArray.Count];
                for (var i = 0; i < servicesArray.Count; i++)
                {
                    names[i] = servicesArray[i]["name"]?.ToString() ?? "";
                }
                return names;
            }
        }

        private void Load()
        {
            var server = _config["server"];
            _accountId = server?["account-id"]?.Value<int>() ?? 0;
            _deviceId = server?["device-id"]?.Value<int>() ?? 0;

            ApiKeyLoadState = ApiKeyState.NotSet;
            var protectedKey = server?["api-key-protected"]?.ToString();
            if (!string.IsNullOrEmpty(protectedKey))
            {
                try
                {
                    _apiKey = AgentSecretProtector.Unprotect(protectedKey);
                    ApiKeyLoadState = ApiKeyState.Loaded;
                }
                catch (AgentSecretProtectionException ex)
                {
                    // Entropy does not yet exist on this machine (fresh install) or was
                    // generated on a different machine.  Treat credentials as unconfigured
                    // so Setup can open and let the operator re-enter / re-protect them.
                    Logger.Warning(ex, "MonitorProfile: could not decrypt api-key-protected — treating as unconfigured.");
                    _apiKey = null;
                    ApiKeyLoadState = ApiKeyState.DecryptionFailed;
                }
            }

            // Refresh token lives in its own file so a crash during write can never corrupt appsettings.json.
            var tokenFilePath = TokenFilePath;
            if (File.Exists(tokenFilePath))
            {
                var protectedToken = File.ReadAllText(tokenFilePath).Trim();
                if (!string.IsNullOrEmpty(protectedToken))
                {
                    try
                    {
                        _refreshToken = AgentSecretProtector.Unprotect(protectedToken);
                    }
                    catch (AgentSecretProtectionException ex)
                    {
                        Logger.Warning(ex, "MonitorProfile: could not decrypt {TokenFile} — treating as unconfigured.", TokenFileName);
                        _refreshToken = null;
                    }
                }
            }
            else
            {
                // One-time migration: if the legacy appsettings.json field is present, move it to the token file.
                var protectedToken = server?["refresh-token-protected"]?.ToString();
                if (!string.IsNullOrEmpty(protectedToken))
                {
                    try
                    {
                        _refreshToken = AgentSecretProtector.Unprotect(protectedToken);
                        Logger.Information("MonitorProfile: migrating refresh token from appsettings.json to {TokenFile}.", TokenFileName);
                        WriteTokenFile(protectedToken);
                    }
                    catch (AgentSecretProtectionException ex)
                    {
                        Logger.Warning(ex, "MonitorProfile: could not decrypt legacy refresh-token-protected — treating as unconfigured.");
                        _refreshToken = null;
                    }
                }
            }

            _updateTime = server?["update-time"]?.ToString();
            _commandPort = server?["command-port"]?.Value<int>() ?? 0;
            _reconnectAttemptMinutes = server?["reconnect-attempt-minutes"]?.Value<int>() ?? 45;
            _accountName = server?["account-name"]?.ToString();
            _groupName = server?["group-name"]?.ToString();
            _deviceName = server?["device-name"]?.ToString();

            // get the base address from the url, stripping any protocol prefix if present since BaseAddress/WebSocketAddress add it back in.
            _baseAddress = server?["url"]?.ToString();
            int index = _baseAddress.IndexOf("//");
            _baseAddress = index >= 0 ? _baseAddress[(index + 2)..] : _baseAddress;

            _apiKeyDirty = false;
            _refreshTokenDirty = false;
            Changed = false;
        }


        public void Save()
        {
            _config = JObject.Parse(File.ReadAllText(MonitorFilePath));

            // NOTE: fetch the existing "server" object and mutate it in place.
            // Do NOT do `_config["server"] = server` with an already-parented token:
            // Json.NET clones a token that already has a parent, leaving the local
            // reference detached so subsequent edits never reach the serialized tree.
            var server = _config["server"] as JObject;
            if (server == null)
            {
                server = new JObject();
                _config["server"] = server;
            }
            _config["version"] = 2;

            server["url"] = BaseAddress.TrimEnd('/');
            server["account-id"] = AccountId;
            server["device-id"] = DeviceId;
            server["update-time"] = UpdateTime;
            server["command-port"] = CommandPort;
            server["account-name"] = AccountName;
            server["group-name"] = GroupName;
            server["device-name"] = DeviceName;

            WriteJson();
            Changed = false;
        }

        /// <summary>
        /// Persists the current refresh token to its dedicated file using an atomic
        /// write (temp file + rename) so a crash mid-write can never produce an empty file.
        /// </summary>
        public void WriteRefreshToken()
        {
            if (string.IsNullOrEmpty(_refreshToken))
                return;
            var protectedToken = AgentSecretProtector.Protect(_refreshToken);
            WriteTokenFile(protectedToken);
            _refreshTokenDirty = false;
        }

        /// <summary>
        /// Atomically writes an already-encrypted token value to <see cref="TokenFilePath"/>.
        /// Writes to a .tmp file first then renames, so the target is never left empty on crash.
        /// </summary>
        private static void WriteTokenFile(string protectedToken)
        {
            var tmp = TokenFilePath + ".tmp";
            File.WriteAllText(tmp, protectedToken);
            File.Move(tmp, TokenFilePath, overwrite: true);
        }

        public void WriteJson()
        {
            var server = _config["server"] as JObject;
            if (server != null && _apiKeyDirty && !string.IsNullOrEmpty(_apiKey))
            {
                server["api-key-protected"] = AgentSecretProtector.Protect(_apiKey);
                _apiKeyDirty = false;
            }

            File.WriteAllText(MonitorFilePath, _config.ToString(Formatting.Indented));
        }

        private static DateTime _lastChangeAt = DateTime.MinValue;

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // FileSystemWatcher commonly fires twice per write on Windows (write-begin + flush).
            // Suppress duplicates within a 500 ms window.
            var now = DateTime.UtcNow;
            lock (LockObject)
            {
                if ((now - _lastChangeAt).TotalMilliseconds < 500)
                    return;
                _lastChangeAt = now;
                _current = null;
            }
            Logger.Information(MonitorFileName + " change detected.");
        }

        private void Raise(EventHandler handler, object sender)
        {
            Changed = true;
            handler?.Invoke(sender, EventArgs.Empty);
        }

    }

    public enum ApiKeyState
    {
        NotSet,
        DecryptionFailed,
        Loaded
    }
}