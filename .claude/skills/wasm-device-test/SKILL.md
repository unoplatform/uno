---
description: Publish an Uno WebAssembly app and serve it on the local network so it can be opened on a phone, tablet, or another machine. Use when testing WASM behaviour that cannot be reproduced on a desktop browser — touch input, mobile GPUs, device refresh rates, small viewports.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Overview

You are executing the **WASM Device Test Skill**. It publishes an Uno Platform WebAssembly app and
serves the published output over HTTP **bound to all network interfaces**, so a phone or tablet on
the same network can load it.

This exists because the two obvious approaches both fail for device testing:

- `dotnet run` serves on `localhost` only — a phone cannot reach it.
- `python -m http.server` binds loopback only *and* does not know the `.wasm` MIME type, so the
  browser cannot use `WebAssembly.instantiateStreaming`.

Use it when the thing under test is device-specific: touch and gesture handling, inertia and fling
behaviour, display refresh rate (many phones idle at 60 Hz and boost to 120 Hz only while touch is
active), GPU rasterization, safe-area insets, or on-screen keyboard behaviour.

---

## Execution Workflow

### Phase 0: Parse User Input

| Input | Meaning | Default |
|---|---|---|
| a project path or name | the WASM head to publish | auto-detect (below) |
| `debug` | publish Configuration=Debug | **Release** |
| `port <n>` | HTTP port | `8123` |
| `no-build` / `serve-only` | skip publish, serve the existing output | publish first |

**Always publish Release unless the user explicitly asks for Debug.** A Debug WebAssembly build is
dominated by interpreter overhead, so any conclusion about smoothness, frame rate, or responsiveness
drawn from it is meaningless.

**Auto-detecting the project.** Look for a WebAssembly head in this order:

1. A project the user named.
2. `**/*.Skia.WebAssembly.Browser/*.csproj` (the Uno Platform repo's own SamplesApp head is
   `src/SamplesApp/SamplesApp.Skia.WebAssembly.Browser/`).
3. Any `.csproj` whose `TargetFramework` contains `browserwasm`, or that references
   `Uno.Sdk` with a `WebAssembly` head.

If more than one candidate exists, ask which one rather than guessing.

### Phase 1: Publish

**CRITICAL**: WASM publish is slow (several minutes, and much longer with AOT). Set a timeout of
20+ minutes and **never cancel it**. Run it in the background and continue with Phase 2 setup.

```bash
dotnet publish <project> -c Release -f <tfm> -p:UnoFastDevBuild=true
```

Notes:

- `publish` is required, not `build` — only publish produces the complete static web asset set.
- In the Uno Platform repo, add `-p:UnoTargetFrameworkOverride=net10.0` (or whichever single TFM you
  are iterating on) to skip redundant cross-targeted outputs.
- The published site root is
  `<project>/bin/<Configuration>/<tfm>/publish/wwwroot`. Verify `index.html` and `_framework/` exist
  before serving; if they do not, the publish did not complete.

If the publish fails, read the error and fix or report it. Do not fall back to serving a stale
output — the user will test the wrong bits and the result will be misleading.

### Phase 2: Serve

```bash
python scripts/serve-wasm.py <wwwroot> [port]
```

Run it in the **background** — it blocks until stopped. The script prints the URLs to use:

```
Serving …/publish/wwwroot
  this machine : http://localhost:8123/
  other devices: http://192.168.1.20:8123/
```

It binds `0.0.0.0`, registers the `.wasm`/`.dat`/`.blat` MIME types, and sends
`Cache-Control: no-store` so a republished app is picked up without the service worker serving a
stale build.

### Phase 3: Verify before handing over the URL

Do not give the user a URL you have not checked. Confirm all three:

```bash
# 1. the app is served
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:<port>/index.html

# 2. .wasm has the right MIME type (must be application/wasm)
curl -s -I "http://localhost:<port>/_framework/$(ls <wwwroot>/_framework/*.wasm | head -1 | xargs basename)" | grep -i content-type

# 3. the LAN address actually answers — this is the one that catches a blocking firewall
curl -s -m 4 -o /dev/null -w "%{http_code}\n" http://<lan-ip>:<port>/index.html
```

The script prints every non-loopback IPv4 address it can find, which on a developer machine usually
includes VPN, WSL, and Hyper-V adapters that a phone cannot reach. Probe them and report the one
that answered, rather than listing all of them.

**If the LAN probe fails**, it is almost always the host firewall. Report it and give the user the
command to fix it — do not attempt it silently, since it needs elevation and changes the host's
network exposure:

```powershell
New-NetFirewallRule -DisplayName "Uno WASM device test" -Direction Inbound -LocalPort <port> -Protocol TCP -Action Allow
```

Remind the user to remove the rule when finished:

```powershell
Remove-NetFirewallRule -DisplayName "Uno WASM device test"
```

### Phase 4: Report

Give the user:

- the **device URL** (the one that passed the probe), and the localhost URL for desktop comparison
- the configuration that was published, so they know whether the numbers mean anything
- a note that first load transfers the full asset set (often 200 MB+ uncompressed) and will be slow
- what you expect to be better or unchanged, if this run is testing a specific change — so a result
  that contradicts it is recognisable as new information rather than noise

Tell the user the server is still running and offer to stop it. It holds the port until stopped.

---

## Notes and Limitations

- **HTTP only.** Browser APIs gated on a secure context (clipboard, geolocation, service workers on
  some browsers, `SharedArrayBuffer`) will not work over a plain LAN address. If the user needs
  those, they need HTTPS with a trusted certificate or a tunnel; say so rather than debugging the
  symptom.
- **Same network required.** Guest/isolated Wi-Fi and many corporate networks block client-to-client
  traffic even when both devices are online. If the phone cannot connect but the desktop can, and
  the firewall rule is present, suspect AP isolation.
- **Not the runtime-test runner.** For automated WASM runtime tests use the `/runtime-tests` skill,
  which drives the test harness and collects results. This skill is for a human driving the app by
  hand on a device.
- **Serving is not deploying.** The service worker caches aggressively; `no-store` covers the normal
  republish case, but if a device seems stuck on an old build, have the user clear site data.
