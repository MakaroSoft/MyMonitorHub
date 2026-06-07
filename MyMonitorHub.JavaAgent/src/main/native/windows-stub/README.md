# Windows JNI Stub

`mymonitorhub_shr.c` is a Windows stub implementation of the `mymonitorhub_shr`
native library. It allows the Java agent to run on Windows for development and
testing without an OpenVMS host, returning realistic dummy data for all four
command handlers (`onSysInfo`, `onServices`, `onService`, `onTopCpu`).

The real OpenVMS C source lives in `JNI/VMS/vms.c` and `JNI/VMS2/vms2.c`.
This stub is **not** a replacement — it is a developer convenience only.

---

## Step 1 — Install a JDK

You need a full **JDK** (not just the JRE) for the `jni.h` header files.

Recommended: **Eclipse Temurin 8 JDK**
- Download: <https://adoptium.net/temurin/releases/?version=8>
- During install, tick **"Set JAVA_HOME variable"**

---

## Step 2 — Install a C compiler

**MinGW-w64 (GCC)** is the easiest option on Windows:

```
winget install MinGW.MinGW
```

Then open a **new** command prompt so `gcc` is on the `PATH`.

Alternatively, open a **Visual Studio Developer Command Prompt** if you have
MSVC installed.

---

## Step 3 — Build the DLL

```
cd MyMonitorHub.JavaAgent\src\main\native\windows-stub
build.bat
```

This produces `mymonitorhub_shr.dll` in the same directory.

---

## Step 4 — Run the Agent

```
java -Djava.library.path="MyMonitorHub.JavaAgent\src\main\native\windows-stub" ^
     -jar MyMonitorHub.JavaAgent\target\mymonitorhub-java-agent-*.jar
```

Or set `java.library.path` in your IDE run configuration to the folder
containing the DLL.

---

## Stub data summary

| Handler | Stub data |
|---|---|
| `onSysInfo` | computerType=`HP AlphaServer DS25`, osVersion=`V8.4-2L2`, cpus=2, lastBoot=`1-May-2026 06:00:00` |
| `onTopCpu` | 4 processes across 2 CPUs: VTXSERVER_1960, ORACLE_DB, HTTPD_SERVER, MAIL_SERVER |
| `onServices` | All configured services reported as Running |
| `onService` | Start/stop acknowledged (OK), service reported as Running after |
