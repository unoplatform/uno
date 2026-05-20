---
uid: Uno.Features.DevServer.EnterpriseNetwork
---

# Dev Server — Enterprise Network Configuration Guide

This page is intended for **IT administrators** managing developer workstations in
constrained corporate environments.  It explains what the Uno Platform Dev Server
does, what network access it requires, and how to configure Windows Firewall — either
manually or via Group Policy — so that developers can use Hot Reload with physical
Android and iOS devices.

---

## What is the Uno Platform Dev Server?

The Dev Server is a local HTTP/WebSocket server that runs on the developer's
workstation during development.  It is started automatically by the IDE (Visual
Studio, VS Code, Rider) or by the `uno-devserver` CLI tool when a developer runs or
deploys an Uno Platform application.

It is the backbone of several Uno Platform Studio features:

| Feature | What the Dev Server enables |
|---------|-----------------------------|
| **Hot Reload** | Push XAML and C# changes to the running app without a full rebuild |
| **Hot Design** | Live visual editing of XAML directly in the running app |
| **Mobile development** | Real-time diagnostics, state inspection, and tooling for Android and iOS |
| **MCP / AI agents** | Exposes development tools to AI coding assistants (Claude Code, Copilot, etc.) |

**The Dev Server is a development-only tool.** It is never started in production
builds and poses no runtime risk to end users.

---

## Network requirements

| Property | Value |
|----------|-------|
| Protocol | HTTP/WebSocket (plain, not TLS) |
| Direction | **Inbound** to the developer's workstation |
| Port | Dynamic (allocated at startup, typically in the 49152–65535 range) |
| Binding | All network interfaces (`0.0.0.0`) |
| Process | `Uno.UI.RemoteControl.Host.exe` (launched by the Dev Server CLI) |
| Lifetime | Active only while the app is running in the IDE — stops when the debug session ends |

Physical devices (Android, iOS) connect back to the workstation over the local
network using the IP address embedded in the app at build time.

---

## Windows Firewall configuration

### Why the default configuration blocks physical devices

The Dev Server CLI spawns `Uno.UI.RemoteControl.Host.exe` with `CreateNoWindow = true`
so that no console window appears on the developer's desktop.  As a side effect,
this flag also suppresses the Windows Firewall interactive dialog that would normally
prompt the user to allow inbound connections the first time a new executable listens
on a port.  Without that dialog, **no Private or Domain rule is ever created**
for the host executable, and physical devices on those network profiles are silently
blocked.

Android emulators (AVD) are unaffected because they communicate through the ADB
bridge, which bypasses the Windows network stack.

### Automatic fix (Uno SDK 6.6+)

Starting with Uno SDK 6.6, the Dev Server CLI automatically attempts to add the
required firewall rule the first time it starts.  A **UAC (User Account Control)
prompt** will appear asking for administrator elevation.  Once approved, the rule is
created for the current version of `Uno.UI.RemoteControl.Host.exe`.

> [!NOTE]
> When developers update the Uno Platform NuGet packages, the path to
> `Uno.UI.RemoteControl.Host.exe` changes (it includes the package version).
> A new UAC prompt will appear the first time the Dev Server starts after an upgrade.

If the developer is not a local administrator or the UAC prompt was dismissed, the
rule must be added by IT as described below.

### Manual rule (single workstation)

Windows Firewall does not support wildcard characters in program paths, so the rule
must target the **exact path** of the installed `Uno.UI.RemoteControl.Host.exe`.
The path includes the NuGet package version and the .NET TFM, for example:

```
%USERPROFILE%\.nuget\packages\uno.winui.devserver\6.6.0\tools\rc\host\net9.0\Uno.UI.RemoteControl.Host.exe
```

To list the versions currently installed on the developer's machine:

```powershell
Get-ChildItem "$env:USERPROFILE\.nuget\packages\uno.winui.devserver" -Directory |
  Select-Object -ExpandProperty Name
```

For each installed version, run the following command in an **elevated PowerShell**
session, substituting the exact path:

```powershell
$hostExe = "$env:USERPROFILE\.nuget\packages\uno.winui.devserver\<version>\tools\rc\host\net<tfm>\Uno.UI.RemoteControl.Host.exe"

New-NetFirewallRule `
  -DisplayName "Uno DevServer (.NET Host)" `
  -Direction Inbound `
  -Action Allow `
  -Program $hostExe `
  -Profile @("Private", "Domain") `
  -Description "Allows the Uno Platform Dev Server to accept Hot Reload connections from physical Android/iOS devices and other local network clients."
```

> [!NOTE]
> The rule must be updated each time developers upgrade the Uno Platform NuGet
> packages, because the path changes with the new version.

To verify the rule was created:

```powershell
Get-NetFirewallRule -DisplayName "Uno DevServer (.NET Host)" |
  Get-NetFirewallApplicationFilter |
  Select-Object -ExpandProperty Program
```

To remove it:

```powershell
Remove-NetFirewallRule -DisplayName "Uno DevServer (.NET Host)"
```

### Group Policy deployment (fleet of workstations)

To deploy the rule organization-wide via **Group Policy**:

1. Open **Group Policy Management** and create or edit a GPO linked to the developer
   workstations' OU.
2. Navigate to:
   `Computer Configuration > Policies > Windows Settings > Security Settings > Windows Defender Firewall with Advanced Security > Inbound Rules`
3. Create a new rule:
   - **Rule type:** Program
   - **Program path:** exact path to `Uno.UI.RemoteControl.Host.exe` in the NuGet cache
     (Windows Firewall does not support wildcards in program paths).
     Deploy a login script that resolves the installed version, for example:
     `%USERPROFILE%\.nuget\packages\uno.winui.devserver\6.6.0\tools\rc\host\net9.0\Uno.UI.RemoteControl.Host.exe`
   - **Action:** Allow the connection
   - **Profile:** ✅ Domain &nbsp; ✅ Private &nbsp; ☐ Public
   - **Name:** `Uno DevServer (.NET Host)`

> [!NOTE]
> The host executable path includes the NuGet package version and the .NET TFM, so it
> changes when developers update the Uno Platform NuGet packages.  The automatic fix
> applied by the CLI at startup covers the developer's own machine (it prompts for UAC
> when a new version is installed).  For GPO-managed machines where the CLI cannot
> elevate, you will need to update the rule after each significant Uno Platform version
> upgrade, or use a login script to detect and update the path automatically.

### Verifying connectivity from the device

After adding the rule, ask the developer to:

1. Connect the Android or iOS device to the same Wi-Fi network as the workstation.
2. Start the app from the IDE.
3. Observe the **Uno Platform Studio indicator** in the app (Hot Reload, Hot Design)
   — it should show **Connected** within a few seconds.

If the indicator still shows a connection failure after the rule is in place, check:

- The device and workstation are on the **same network segment** (no client isolation
  or VLAN separation between them).
- No **additional firewall, proxy, or network appliance** is blocking traffic between
  the device IP and the workstation IP on the allocated port.

---

## Proxy and network appliance considerations

The Dev Server communicates directly over TCP/WebSocket between the device and the
workstation.  It does **not** use HTTP proxies.  If your network routes all traffic
through an intercepting proxy:

- Ensure the proxy allows **WebSocket upgrades** (`Upgrade: websocket` header).
- Add the workstation's IP or hostname to the proxy bypass list for the devices'
  network segment if possible.

The Dev Server does not use TLS by default.  Traffic is plain HTTP/WebSocket over
the local network and contains only development artifacts (XAML/C# source deltas,
UI state, diagnostics) — no user data, credentials, or production assets.

---

## See also

- [Hot Reload — Troubleshooting](xref:Uno.Features.HotReload)
- [Dev Server Diagnostics (`disco`)](xref:Uno.Features.DevServerDisco)
