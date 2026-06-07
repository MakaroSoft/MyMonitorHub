# MyMonitorHub Java Agent

Java 1.8 port of the MyMonitorHub monitoring agent. Runs as a console application (no Windows Service hosting).

## Requirements

- Java 1.8 (JDK 8)
- Maven 3.x

## Build

```bash
mvn package
```

This produces `target/mymonitorhub-java-agent-1.0.0.jar` — a self-contained fat JAR.

## Run

```bash
java -jar target/mymonitorhub-java-agent-1.0.0.jar
```

`appsettings.json` is read from the current working directory by default. Override with the environment variable:

```bash
set MYMONITORHUB_AGENT_CONFIG_DIR=C:\path\to\config
java -jar mymonitorhub-java-agent-1.0.0.jar
```

## Configuration (`appsettings.json`)

The Java agent uses the same `appsettings.json` structure as the C# agent with two differences:

| C# field | Java field | Notes |
|---|---|---|
| `api-key-protected` | `api-key` | Plain text — DPAPI is Windows/.NET only |
| `refresh-token-protected` | `refresh-token` | Plain text — updated automatically on refresh |

### Self-signed certificate

If the hub server uses a self-signed certificate (common in development), set:

```json
"ssl-trust-all": true
```

> **Warning:** Do not enable `ssl-trust-all` in production.

### Plugin class names

The Java agent loads plugins by class name using `Class.forName()`. Use fully-qualified Java class names instead of C# assembly names:

| C# class-name | Java class-name |
|---|---|
| `MyMonitorHub.Agent.Plugins.DiskSpaceMonitor` | `com.makarosoft.mymonitorhub.agent.plugins.DiskSpaceMonitor` |
| `MyMonitorHub.Agent.Plugins.HttpVortexMonitor` | `com.makarosoft.mymonitorhub.agent.plugins.HttpVortexMonitor` |
| `MyMonitorHub.Agent.Plugins.CommandWatch` | `com.makarosoft.mymonitorhub.agent.plugins.CommandWatch` |
| `MyMonitorHub.Agent.Plugins.ServiceMonitor` | `com.makarosoft.mymonitorhub.agent.plugins.ServiceMonitor` |

The `assembly` field in plugin entries is ignored (all plugins ship inside the same JAR).

## Features vs C# agent

| Feature | Java agent |
|---|---|
| Event collection and batch POST | ✅ Same 60s interval, same JSON shape |
| JWT auth / refresh | ✅ Same endpoints, manual `exp` parsing |
| WebSocket hub channel | ✅ `Java-WebSocket` 1.5.x, TLS 1.2 |
| Local TCP command port (MCM) | ✅ Byte-for-byte protocol compatible |
| Disk space monitoring | ✅ `java.io.File` API |
| HTTP endpoint monitoring | ✅ `HttpURLConnection` |
| Windows service monitoring | ✅ `sc query` via `Runtime.exec` (Windows only) |
| Command watch | ✅ Pings local command port |
| TopCpu via WebSocket | ✅ OSHI 5.8.x |
| SysInfo via WebSocket | ✅ OSHI 5.8.x |
| Self-heal restart | ✅ `Runtime.halt(1)` |
| IIS App Pool monitoring | ❌ Skipped (IIS-specific) |
| DPAPI secret protection | ❌ Not available in Java; use plain text |
| Windows Service hosting | ❌ Console app only (by design) |
| Plugin extensibility | ✅ Any class extending `AbstractPlugin` on the classpath |

## Logging

Logs are written to both the console and a rolling daily file (`monitor-<date>.log`). Configure via `logback.xml` in the working directory or on the classpath.
