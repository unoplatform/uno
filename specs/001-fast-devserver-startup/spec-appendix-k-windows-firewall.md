# Appendix K: Windows Firewall — Private Network Support

> **Parent**: [Main Spec](spec.md)
> **Related**: [Manual QA](spec-appendix-h-manual-qa.md)

Physical Android/iOS devices and other remote clients (WSL, VMs) cannot connect to
the DevServer when the development machine is on a **Private** Wi-Fi network on
Windows, while the emulator (AVD) and loopback clients are unaffected.
This appendix explains the root cause, the fix, and how to verify it.

---

## 1. Root Cause

### 1.1 Windows Firewall profile model

Windows Firewall classifies every network interface as one of three profiles:
**Domain**, **Private**, or **Public**.  Wi-Fi and Ethernet home/office networks
are normally classified as **Private**.  Inbound Allow rules can be scoped to one
or more profiles.

### 1.2 How the regression was introduced

The DevServer CLI has always launched `Uno.UI.RemoteControl.Host.exe` directly on
Windows (not `dotnet.exe <dll>`).  The real change that introduced the regression
is `CreateNoWindow = true` on the spawned process.

| Era | How the host was launched | `CreateNoWindow` | Firewall dialog shown |
|-----|--------------------------|------------------|-----------------------|
| Before DevServer CLI | `Uno.UI.RemoteControl.Host.exe` from IDE | `false` (visible window) | ✅ Yes — user clicked *Allow* → Private + Public rule |
| DevServer CLI (current) | `Uno.UI.RemoteControl.Host.exe` via CLI | `true` (hidden) | ❌ No — dialog never appears |

`CreateNoWindow = true` tells Windows not to create a console window for the child
process.  As a side effect, it also suppresses the Windows Firewall interactive dialog
that would normally prompt the user to allow inbound connections for a new executable.
Without that dialog, **no Private or Domain rule is ever created** for
`Uno.UI.RemoteControl.Host.exe`, even though the exe itself is unchanged.

Result: physical Android/iOS devices on a Private Wi-Fi network are silently blocked
by the firewall before any TCP connection reaches the DevServer.

### 1.3 Why the emulator is unaffected

Android emulators (AVD) connect to the host via `10.0.2.2`, which routes through
the ADB USB/TCP bridge.  This path bypasses the Windows network stack entirely —
the firewall does not apply.  Loopback clients (Desktop, WASM) are similarly
unaffected because loopback traffic is never subject to inbound firewall rules.

### 1.4 Why the Domain profile matters too

Developers working on corporate networks are classified under the **Domain** profile
(Active Directory membership).  This is a common setup: the machine is domain-joined
but the developer has local admin rights and tests with physical devices on the same
LAN.  The SDK installer rule covers only Public, so Domain is equally blocked.

Any client that reaches the DevServer over a real network interface (physical Android/iOS
device, WSL, VM, browser on a second machine) is subject to the same gap — regardless
of whether the profile is Private or Domain.  The fix therefore covers both.

---

## 2. Fix

### 2.1 Principle

The DevServer CLI (`uno-devserver start`) is the single launch point for all IDEs
(VS, VS Code, Rider) and for MCP/agent-initiated starts.  It is therefore the
correct and only location for the fix.

Before spawning the DevServer host, `StartCommandHandler.RunAsync()` calls
`WindowsFirewallHelper.EnsureFirewallRuleAsync(hostExePath, ...)`.  This helper:

1. Checks whether a rule named `"Uno DevServer (.NET Host)"` already exists (by display name, exit-code only).
2. If not, launches an elevated `netsh` process (UAC prompt) to add one for `hostExePath`.
3. Logs the outcome clearly; gracefully degrades if UAC is declined.

The check is **idempotent by rule name** — the UAC prompt appears at most once per display-name lifetime.
Note: if the Uno package is upgraded, the existing rule (pointing at the previous `Host.exe` path) is
found by name and the new path is not automatically added.  The user must delete the old rule to trigger
a new prompt (see the IT admin guide).

### 2.2 The firewall rule

```
Name:      Uno DevServer (.NET Host)
Direction: Inbound
Action:    Allow
Program:   <Uno.UI.RemoteControl.Host.exe path>
Profile:   Private, Domain
```

The rule targets `Uno.UI.RemoteControl.Host.exe` — the actual executable that opens
the inbound port — not `dotnet.exe`.  This is the minimum-privilege approach: only
the specific DevServer process is whitelisted, not every .NET process on the machine.

The rule covers **Private** (home/office Wi-Fi) and **Domain** (corporate
Active Directory networks where the developer has local admin rights).
**Public** is intentionally omitted — Windows already allows inbound connections
for known executables on Public when the user accepted the dialog in earlier versions.

### 2.3 Files changed

| File | Change |
|------|--------|
| `src/Uno.UI.DevServer.Cli/StartCommandHandler.cs` | Call `WindowsFirewallHelper.EnsureFirewallRuleAsync()` before host spawn on Windows |
| `src/Uno.UI.DevServer.Cli/Helpers/WindowsFirewallHelper.cs` | New file — Windows-only helper (`[SupportedOSPlatform("windows")]`) |

### 2.4 `WindowsFirewallHelper` algorithm

```
1. Opt-out check
   If UNO_DEVSERVER_SKIP_FIREWALL_CHECK=1 (or "true", case-insensitive) → log debug, return.
   Useful for CI runners, GPO-managed machines, or any environment where elevation
   is not permitted.

2. Check for existing rule by name (language-agnostic exit-code check)
   Run: netsh advfirewall firewall show rule name="Uno DevServer (.NET Host)" dir=in
   Timeout: 10 s.
   Exit 0  → rule found → return (no UAC, no log noise).
   Non-0   → rule absent → continue.
   (No output parsing — avoids any dependency on localized netsh field labels.)

3. Add the rule (rule absent)
   Log informational: "A UAC prompt will appear — this happens once per machine."
   Run elevated: netsh advfirewall firewall add rule
                   name="Uno DevServer (.NET Host)"
                   dir=in action=allow
                   program="<Uno.UI.RemoteControl.Host.exe path>"
                   enable=yes profile=private,domain
     via Process.Start { Verb="runas", UseShellExecute=true }
   Timeout: 60 s (allows time for UAC interaction).
   Exit 0  → log success.
   Non-0 or exception → log warning + manual PowerShell command, return.

4. Graceful degradation (UAC declined or netsh failed)
   DevServer continues to start normally.
   Warning logged with manual workaround (host exe path shown explicitly):
     New-NetFirewallRule -DisplayName "Uno DevServer (.NET Host)" `
       -Direction Inbound -Action Allow `
       -Program "<Uno.UI.RemoteControl.Host.exe path>" `
       -Profile @("Private", "Domain")
```

### 2.5 Startup impact

- **Happy path (rule exists):** one `netsh show` call (exit-code check, no output parsing) — < 100 ms.
- **First run (rule absent):** UAC prompt + one `netsh add` call — one-time per machine.
- **No solution or non-Windows:** the entire block is skipped.

---

## 3. Requirements

### Functional

| ID | Requirement |
|----|-------------|
| FR-FW1 | On Windows, `StartCommandHandler` must ensure a Private+Domain inbound Allow rule exists for `Uno.UI.RemoteControl.Host.exe` before spawning the host. |
| FR-FW2 | The check must be idempotent — if the rule already exists no elevation is requested. |
| FR-FW3 | If the rule is absent, the CLI must prompt for UAC elevation via an elevated `netsh` invocation. |
| FR-FW4 | If UAC is declined or `netsh` fails, the CLI must log a warning with the manual PowerShell command and continue startup normally (no crash, no exception propagation). |
| FR-FW5 | The rule must target the exact `Uno.UI.RemoteControl.Host.exe` path passed by the caller (`hostExePath`). |
| FR-FW6 | The rule display name must be `"Uno DevServer (.NET Host)"` so it is recognizable in the Windows Firewall UI. |

### Non-functional

| ID | Requirement |
|----|-------------|
| NFR-FW1 | Happy-path overhead (rule exists) must be < 100 ms. |
| NFR-FW2 | The helper must compile without warnings on non-Windows platforms (guarded by `[SupportedOSPlatform("windows")]` and `OperatingSystem.IsWindows()` at the call site). |
| NFR-FW3 | No persistent CLI-side state is written to disk; the firewall rule itself is the persistence mechanism. |

---

## 4. Verification

### Automated tests

`Given_WindowsFirewallHelper.cs` — pure-logic tests (no process spawning):

| Test | Scenario | Expected |
|------|----------|----------|
| `IsOptedOut_WhenSetToOne_ReturnsTrue` | `UNO_DEVSERVER_SKIP_FIREWALL_CHECK=1` | `true` |
| `IsOptedOut_WhenSetToTrueCaseInsensitive_ReturnsTrue` | `UNO_DEVSERVER_SKIP_FIREWALL_CHECK=TRUE` | `true` |
| `IsOptedOut_WhenSetToZero_ReturnsFalse` | `UNO_DEVSERVER_SKIP_FIREWALL_CHECK=0` | `false` |
| `IsOptedOut_WhenAbsent_ReturnsFalse` | env var not set | `false` |
| `EnsureFirewallRuleAsync_WhenOptedOut_ReturnsWithoutError` | `SKIP=1`, no netsh available | completes without throwing |

The netsh calls themselves (`RuleExistsAsync`, `AddFirewallRuleAsync`) interact with
the OS process layer and are validated through manual QA (see below) and the CI
Windows runner when the DevServer is exercised end-to-end.

### Manual QA

> **Prerequisite:** remove any pre-existing Uno DevServer firewall rule:
> ```powershell
> Get-NetFirewallRule | Where-Object { $_.DisplayName -like "Uno DevServer*" } | Remove-NetFirewallRule
> ```

| # | Scenario | Steps | Acceptance criteria |
|---|----------|-------|---------------------|
| K1 | First run — rule absent | Run `uno-devserver start --httpPort 12345 --solution <path>` | UAC prompt appears; after accepting, `Get-NetFirewallRule -DisplayName "Uno DevServer (.NET Host)"` returns the rule with Profile = Private, Domain. |
| K2 | Subsequent run — rule present | Run `uno-devserver start` again | No UAC prompt; startup proceeds normally at normal speed. |
| K3 | Physical Android device | Deploy app to a physical device on the same Wi-Fi network | Hot Reload connection is established (DevServer indicator shows Connected). |
| K4 | UAC declined | Remove the rule, run `uno-devserver start`, decline UAC | Warning is logged with the manual `New-NetFirewallRule` command; DevServer starts normally. |
| K5 | Desktop / WASM app | Run DevServer for a Skia or WASM project (no physical device) | DevServer starts; if rule already exists no prompt, if absent one-time UAC prompt (acceptable — future mobile use is possible on the same machine). |

---

## 5. Out of Scope

- macOS / Linux (no Windows Firewall; those platforms are unaffected)
- Modifying `EntryPoint.cs` (the backward-compat VS-only path that spawns `dotnet + dll` directly — intentionally left unchanged)
- Port-based firewall rules (program-based rules are more precise and match the existing SDK rule pattern)
- Automatic rule cleanup on NuGet package uninstall
- **Group Policy–managed environments:** when an organization's GPO blocks `netsh` or periodically reverts user-created firewall rules, the automatic fix will fail gracefully (warning logged, manual command shown). The correct resolution in this case is for the IT administrator to deploy the rule via GPO for `Uno.UI.RemoteControl.Host.exe` (inbound Allow, Private and Domain profiles). This is documented in the public troubleshooting guide.
