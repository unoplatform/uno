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
| Process | `dotnet.exe` (the .NET SDK runtime) |
| Lifetime | Active only while the app is running in the IDE — stops when the debug session ends |

Physical devices (Android, iOS) connect back to the workstation over the local
network using the IP address embedded in the app at build time.

---

## Windows Firewall configuration

### Why the default configuration blocks physical devices

The .NET SDK installer creates an inbound Allow rule for `dotnet.exe` that covers
the **Public** firewall profile only.  Corporate workstations joined to Active
Directory are assigned the **Domain** profile, and home or office Wi-Fi networks are
assigned the **Private** profile.  Neither is covered by the SDK's default rule,
so physical devices on the same network segment are silently blocked.

Android emulators (AVD) are unaffected because they communicate through the ADB
bridge, which bypasses the Windows network stack.

### Automatic fix (Uno SDK 6.7+)

Starting with Uno SDK 6.7, the Dev Server CLI automatically attempts to add the
required firewall rule the first time it starts.  A **UAC (User Account Control)
prompt** will appear asking for administrator elevation.  Once approved, the rule is
permanent and no further prompts appear.

If the developer is not a local administrator or the UAC prompt was dismissed, the
rule must be added by IT as described below.

### Manual rule (single workstation)

Run the following command in an **elevated PowerShell** session on the developer's
machine:

```powershell
New-NetFirewallRule `
  -DisplayName "Uno DevServer (.NET Host)" `
  -Direction Inbound `
  -Action Allow `
  -Program (Get-Command dotnet).Source `
  -Profile @("Private", "Domain") `
  -Description "Allows the Uno Platform Dev Server to accept Hot Reload connections from physical Android/iOS devices and other local network clients."
```

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
   - **Program path:** `%ProgramFiles%\dotnet\dotnet.exe`
     *(adjust if your organization installs .NET to a non-default location)*
   - **Action:** Allow the connection
   - **Profile:** ✅ Domain &nbsp; ✅ Private &nbsp; ☐ Public *(Public is already covered by the SDK installer)*
   - **Name:** `Uno DevServer (.NET Host)`

> [!NOTE]
> If developers install .NET to a non-standard location (e.g., `%LOCALAPPDATA%\Programs\dotnet`),
> the program path in the rule must match.  You can use a wildcard path or deploy
> a script that reads `(Get-Command dotnet).Source` on each machine.

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

## Port range considerations

The Dev Server allocates a port dynamically in the ephemeral range
(49152–65535).  If your network policy requires explicit port rules rather than
program-based rules, you can restrict the allowed range using the
`UnoRemoteControlPort` MSBuild property in the developer's `.csproj`:

```xml
<PropertyGroup>
  <!-- Fix the Dev Server port to simplify firewall rules -->
  <UnoRemoteControlPort>57512</UnoRemoteControlPort>
</PropertyGroup>
```

With a fixed port, the firewall rule can target that specific port instead of a
program path:

```powershell
New-NetFirewallRule `
  -DisplayName "Uno DevServer (fixed port)" `
  -Direction Inbound `
  -Action Allow `
  -Protocol TCP `
  -LocalPort 57512 `
  -Profile @("Private", "Domain")
```

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
