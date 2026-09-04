# Spec 059: Desktop Activation & Single-Instance Redirection (Skia Desktop)

> **Status**: Not started — specification only. The preceding change routed every activation
> through `Microsoft.Windows.AppLifecycle.AppInstance` and wired Android, iOS/tvOS and
> WebAssembly to it, but deliberately shipped **no** desktop activation and **no**
> single-instance redirection. This document is the hand-off so the follow-up can be picked
> up cleanly.
> **Author**: Martin Zikmund
> **Date**  : 2026-09-04
> **Targets**: Skia Desktop — Win32, macOS, Linux/X11, Linux FrameBuffer
> **Reference implementation**: the public `microsoft/WindowsAppSDK` repository, `dev/AppLifecycle/`

### Reading Convention

This spec distinguishes **current behavior** (what the code does today) from **target behavior**
(what the follow-up delivers). **The code is the source of truth for current behavior** — where
this spec describes Uno Platform internals it must match the sources under `src/`; discrepancies
are spec bugs.

Every claim about the Windows App SDK is cited as `dev/AppLifecycle/<file>:<line>`, relative to
the root of the public Windows App SDK repository. Those citations were read against a local
checkout and are the normative reference for the contract this spec asks to be reproduced.

Citations into Uno Platform sources name the **file and member**, not a line number, wherever the
file is under active change — line numbers in this repo go stale between commits and turn into spec
bugs. Line numbers are given only for files that are stable anchors.

---

## Executive Summary

### What exists today

`Microsoft.Windows.AppLifecycle.AppInstance` (`src/Uno.WinRT/Microsoft/Windows/AppLifecycle/AppInstance.cs`)
is the single funnel every platform host reports activations to, via the internal
`SetOrRaiseActivation(AppActivationArguments)`. `GetActivatedEventArgs()` never returns `null` and
never throws: with no activation payload it manufactures a `Launch` argument over
`Environment.CommandLine`. `Application.OnLaunched` always receives plain `ActivationKind.Launch`
arguments (`Application.InvokeOnLaunched` in `src/Uno.UI/UI/Xaml/Application.cs`), matching WinUI's
`FrameworkApplication`, and the real payload is read back from `AppInstance`.

Three hosts feed that funnel today:

| Host | File | Cold start | While running |
|------|------|:---:|:---:|
| Android | `NativeApplication.ReportActivation` (`src/Uno.UI.Runtime.Skia.Android/UI/Xaml/NativeApplication.cs`) | ✅ | ✅ |
| iOS / tvOS | `AppleUIKitActivation.Report` (`src/Uno.UI.Runtime.Skia.AppleUIKit/AppleUIKitActivation.cs:59`) | ✅ | ✅ |
| WebAssembly | `WebAssemblyBrowserHost.TryReportProtocolActivation` (`…/Hosting/WebAssemblyBrowserHost.cs:174-191`) | ✅ | n/a |
| **Win32 / macOS / X11 / FrameBuffer** | **none** | ❌ | ❌ |

`ActivationRegistrationManager` is entirely `[Uno.NotImplemented]` on every target
(`src/Uno.WinRT/Generated/3.0.0.0/Microsoft.Windows.AppLifecycle/ActivationRegistrationManager.cs`).
`AppInstance.Restart(string)` is the only member of `AppInstance` still living in the generated
stub as `[Uno.NotImplemented]`
(`src/Uno.WinRT/Generated/3.0.0.0/Microsoft.Windows.AppLifecycle/AppInstance.cs`); every other
member is gated out with `#if false` and implemented by hand.

### What this follow-up adds

1. **A verbatim port of the Windows App SDK command-line activation contract**, so that a scheme
   registered by Windows App SDK tooling and one registered by Uno Platform produce and consume the
   *same* command line — `App.exe "----ms-protocol:<uri>"`.
2. **Per-target URL ingestion** on the four desktop hosts: Win32 command line + running-instance
   delivery, macOS `application:openURLs:`, Linux/X11 command line + XDG desktop entry + D-Bus
   activation, Linux FrameBuffer argument classification only.
3. **A real `ActivationRegistrationManager`**, per target, with an explicit "unsupported" story
   where the OS only accepts manifest-time registration.
4. **Single-instance redirection**, reproducing the Windows App SDK *contract* over a
   cross-platform seam — and doing so as a staged behaviour change, because today
   `FindOrRegisterForKey` unconditionally claims the key for the current instance.
5. **`AppInstance.Restart(string)`**.
6. **The two activation-args types desktop needs and Uno Platform does not have**:
   `FileActivatedEventArgs` and `StartupTaskActivatedEventArgs`.

### Target capability matrix

| Capability | Win32 | macOS | X11 | FrameBuffer | (Android) | (iOS) | (WASM) |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Cold-start protocol activation | ✅ | ✅ | ✅ | ❌ | ✅ today | ✅ today | ✅ today |
| Protocol activation while running | ✅ | ✅ | ✅ | ❌ | ✅ today | ✅ today | ❌ by design |
| File-type activation | ✅ | ✅ | ✅ | ❌ | ➖ | ➖ | ❌ |
| Startup-task activation | ✅ | ⚠️ login item | ✅ autostart | ❌ | ➖ | ➖ | ❌ |
| `RegisterForProtocolActivation` | ✅ HKCU | ✅ LaunchServices | ✅ `.desktop` | ❌ | ❌ manifest | ❌ manifest | ✅ folds `RegisterCustomScheme` |
| `RegisterForFileTypeActivation` | ✅ HKCU | ⚠️ build-time only | ✅ `.desktop` | ❌ | ❌ manifest | ❌ manifest | ❌ |
| `RegisterForStartupActivation` | ✅ HKCU `Run` | ✅ login item | ✅ autostart | ❌ | ❌ | ❌ | ❌ |
| Single-instance redirection | ✅ | ✅ inherent | ✅ | ❌ | ✅ inherent | ✅ inherent | ✅ inherent |
| `Restart` | ✅ | ✅ | ✅ | ✅ | ⚠️ | ❌ | ⚠️ reload |

Legend: ✅ delivered by this spec · ⚠️ partial, see the relevant section · ❌ reports unsupported ·
➖ out of scope (already covered by the platform's own manifest activation).

---

## 1. The Windows App SDK command-line contract

This is the load-bearing part of the spec. Every other desktop piece is plumbing around it.

### 1.1 Why it must be verbatim

The Windows shell does not pass structured activation data to an unpackaged process. It runs a
command line. The Windows App SDK therefore invented a wire format on the command line, and both
the *writer* (`ActivationRegistrationManager`, which puts the template in the registry) and the
*reader* (`AppInstance.GetActivatedEventArgs`, which parses it back) must agree on it.

Two consequences make a verbatim port non-negotiable:

- **A registration outlives the SDK that wrote it.** An app that was registered by Windows App SDK
  tooling — or by a previous Windows App SDK build of the same app — leaves `HKCU` entries behind.
  If Uno Platform parses a different format, those registrations silently stop working after a
  migration to Uno Platform.
- **Mixed-toolchain deployments exist.** An installer, a group policy, or a sibling app may register
  the scheme. Uno Platform must consume whatever the documented Windows App SDK format produces.

### 1.2 The grammar

Four constants define it (`dev/AppLifecycle/ActivationRegistrationManager.h:10-14`):

```cpp
static PCWSTR c_argumentPrefix{ L"----" };
static PCWSTR c_argumentSuffix{ L":" };
static PCWSTR c_msProtocolArgumentString{ L"ms-protocol" };
static PCWSTR c_pushProtocolArgumentString{ L"WindowsAppRuntimePushServer" };
static PCWSTR c_appNotificationProtocolArgumentString{ L"AppNotificationActivated" };
```

so a single command-line argument carrying an activation is:

```
----<kind>:<data>
^^^^      ^^
│         │└─ data — arbitrary, and may itself contain ':'
│         └── suffix
└──────────── prefix, exactly 4 characters
```

`GenerateCommandLine` (`dev/AppLifecycle/ActivationRegistrationManager.cpp:21-28`) assembles the
whole command line, quoting the activation argument as one token:

```cpp
// Example: C:\some\path\App.exe "----ms-protocol:myscheme:some=data&some=other"
return wil::str_printf<std::wstring>(L"%s \"%s%s%s%s\"", exePath.c_str(), c_argumentPrefix,
    c_msProtocolArgumentString, c_argumentSuffix, argumentData.c_str());
```

Note the shape: the prefix, the *literal* kind `ms-protocol`, the suffix, then the data.
`ms-protocol` is always what gets **written**; the push and app-notification kinds are only ever
**read** (§1.4).

### 1.3 The parser, exactly

`GetActivationArguments` (`dev/AppLifecycle/AppInstance.cpp:35-64`):

```cpp
std::wstring_view fullArgument = argv[index];
auto protocolQualifier = wil::str_printf<std::wstring>(L"%s%s%s", c_argumentPrefix, activationKind, c_argumentSuffix);

auto argStart = fullArgument.find(protocolQualifier);
if (argStart == std::wstring::npos) { continue; }

// Push past the '----' commandline argument prefix.
argStart += 4;

std::wstring argument{ fullArgument.substr(argStart) };

// We explicitly use find_first_of here, so that the resulting data may contain : as a valid character.
auto argsDelim = argument.find_first_of(L':');
if (argsDelim == std::wstring::npos) { return { argument, L"" }; }

return { argument.substr(0, argsDelim), argument.substr(argsDelim + 1) };
```

Five behaviours the Uno Platform port must reproduce **exactly**:

| # | Behaviour | Source |
|---|-----------|--------|
| P1 | The command line is tokenized with `CommandLineToArgvW` semantics, then each argument is inspected in order. The first argument that matches wins. | `AppInstance.cpp:37-61`, `:70` |
| P2 | The match is a **substring search** (`find`), not a prefix test. An argument with leading characters before `----ms-protocol:` still matches, and the offset is taken from the match position. | `AppInstance.cpp:42` |
| P3 | Exactly **4 characters** are skipped for the prefix — a hard-coded `argStart += 4`, not `protocolQualifier.length()`. So what remains starts at the *kind*, suffix included. | `AppInstance.cpp:49` |
| P4 | The remainder is split on the **first** `:` (`find_first_of`). This is deliberate and commented: **the data may itself contain colons**, which it always does for a URI (`myscheme:some=data`). A greedy or last-colon split breaks every real payload. | `AppInstance.cpp:53-54` |
| P5 | A remainder with no `:` at all yields kind = the whole remainder, data = empty string. | `AppInstance.cpp:55-58` |

Worked example, `App.exe "----ms-protocol:myscheme:host/path?a=b:c"`:

| Step | Value |
|------|-------|
| `argv[1]` | `----ms-protocol:myscheme:host/path?a=b:c` |
| `find("----ms-protocol:")` | `0` |
| `+= 4` | `4` |
| `argument` | `ms-protocol:myscheme:host/path?a=b:c` |
| `find_first_of(':')` | `11` |
| result | kind `ms-protocol`, data `myscheme:host/path?a=b:c` |

The data is then handed to `ProtocolActivatedEventArgs` verbatim as the activation URI
(`AppInstance.cpp:538`).

### 1.4 The three markers, in order

`ParseCommandLine` (`dev/AppLifecycle/AppInstance.cpp:66-84`) tries three kinds and returns the
first that produces a non-empty kind. The order is fixed at `dev/AppLifecycle/AppInstance.cpp:72`:

```cpp
PCWSTR activationKinds[] = { c_msProtocolArgumentString, c_pushProtocolArgumentString, c_appNotificationProtocolArgumentString };
```

1. `ms-protocol`
2. `WindowsAppRuntimePushServer`
3. `AppNotificationActivated`

`GetActivatedEventArgs` then normalizes (2) and (3) into (1): it synthesizes an
`ms-encodedlaunch` URI for the push or app-notification contract, optionally appending a
`&payload=` extracted from a `-Payload:` sub-argument, and rewrites the kind to `ms-protocol`
before the shared protocol path runs (`AppInstance.cpp:510-533`; the `-Payload:` constant is at
`AppInstance.cpp:30`).

**Uno Platform decision.** Uno Platform has no push-notification or app-notification stack, so
markers (2) and (3) cannot be *honoured*. They must still be **recognised**, for one reason:
without recognising them, the whole `----WindowsAppRuntimePushServer:...` token would fall through
to `OnLaunched` as if the user had typed it as a launch argument. The port therefore scans all
three markers in the documented order, and for (2)/(3) logs at `Warning` and falls through to a
plain `Launch` with the marker token **removed** from the arguments the app sees — the same "Uno
Platform's transport detail is not the app's launch argument" rule the WebAssembly host already
applies to the `unoprotocolactivation` query key
(`src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Hosting/WebAssemblyBrowserHost.cs:185`).

### 1.5 `ms-encodedlaunch`: what it is and whether Uno Platform needs it

**The mechanism.** Two schemes are reserved (`dev/AppLifecycle/ValueMarshaling.h:7-8`):
`ms-launch` and `ms-encodedlaunch`. `GenerateEncodedLaunchUri`
(`dev/AppLifecycle/ValueMarshaling.h:20-26`) builds

```
ms-encodedlaunch:App/?ContractId=Windows.File&Verb=open&File=%1
```

Each activation-args type implements `IInternalValueMarshalable::Serialize()`
(`dev/AppLifecycle/ValueMarshaling.h:15-18`) to append its own fields to that URI:

| Contract id | Type | Extra query fields | Source |
|---|---|---|---|
| `Windows.Launch` | `LaunchActivatedEventArgs` | `&Arguments=` | `dev/AppLifecycle/LaunchActivatedEventArgs.h:12`, `:31-35` |
| `Windows.Protocol` | `ProtocolActivatedEventArgs` | `&Uri=` | `dev/AppLifecycle/ProtocolActivatedEventArgs.h:13`, `:32-36` |
| `Windows.File` | `FileActivatedEventArgs` | `&Verb=`, `&File=` | `dev/AppLifecycle/FileActivatedEventArgs.h:13`, `:94-114` |
| `Windows.StartupTask` | `StartupActivatedEventArgs` | `&TaskId=` | `dev/AppLifecycle/StartupActivatedEventArgs.h:12`, `:31-35` |

The consumption side is the mirror image. `GetActivatedEventArgs` builds a
`ProtocolActivatedEventArgs` from the command-line data, then checks whether its scheme is
`ms-encodedlaunch` (`IsEncodedLaunch`, `dev/AppLifecycle/ExtensionContract.h:33-36`) and, if so,
replaces the protocol args with the *real* typed args (`AppInstance.cpp:546-549` →
`GetEncodedLaunchActivatedEventArgs`, `AppInstance.cpp:86-97` → `DecodeActivatedEventArgs`,
`dev/AppLifecycle/ExtensionContract.h:38-64`). `DecodeActivatedEventArgs` looks up the
`ContractId` query value in a static table of factories
(`c_extensionMap`, `dev/AppLifecycle/ExtensionContract.h:23-31`) and calls the matching
`Deserialize`. It falls back to `{ Protocol, nullptr }` when nothing matches
(`ExtensionContract.h:63`), which is exactly why the caller has the "let the caller args pass
through if nothing was determined here" guard at `AppInstance.cpp:90-94`.

**Why the indirection exists.** A Windows shell verb registration is a *single fixed string per
ProgId*: `HKCU\Software\Classes\<progId>\shell\<verb>\command` holds one `REG_SZ` default value
(`dev/AppLifecycle/Association.cpp:249-262`), and the only substitution the shell performs is
`%1` — the constant is literally `c_commandLineArgumentFormat{ L"%1" }`
(`dev/AppLifecycle/Association.h:23`). There is no way to say "and also tell me it was the *file*
contract, opened with the *print* verb". So the typed payload is smuggled through a URI that the
`ms-protocol` transport can carry, and the ProgId's command template becomes:

| Registration | Registered command template | Source |
|---|---|---|
| Protocol | `App.exe "----ms-protocol:%1"` | `ActivationRegistrationManager.cpp:173-174` |
| File type, per verb | `App.exe "----ms-protocol:ms-encodedlaunch:App/?ContractId=Windows.File&Verb=<verb>&File=%1"` | `ActivationRegistrationManager.cpp:61-63` |
| Startup task | `App.exe "----ms-protocol:ms-encodedlaunch:App/?ContractId=Windows.StartupTask&TaskId=<taskId>"`, stored as an `HKCU\...\Run` value | `ActivationRegistrationManager.cpp:98-105` |

Note the `supportCommandTemplates` flag threaded through `FileActivatedEventArgs`
(`dev/AppLifecycle/FileActivatedEventArgs.h:19-46`, `:101-109`): when serializing a *template* it
skips both the `StorageFile::GetFileFromPathAsync` resolution and the URI-escaping of the path,
precisely so the literal `%1` survives into the registry. The reader path escapes normally.

**Does Uno Platform need it? Yes — and the answer splits.**

- **Decoding: required, and required early.** Not because Uno Platform writes such URIs, but
  because it will *receive* them. Any app previously registered by Windows App SDK tooling has file
  and startup templates already sitting in `HKCU`. An Uno Platform build of that app is launched
  with exactly the command lines above. Without the decode path it sees an opaque
  `ms-encodedlaunch:` protocol activation and cannot tell it is a file open. Decoding is also
  cheap: a URI parse, a query lookup, and a 4-entry factory table.
- **Encoding: required once file-type or startup registration ships** (Phase 3), because that is
  the only way to express those contracts through a shell verb.
- **The `ms-launch` delegate-execute COM registration: not needed, and should not be ported.**
  `RegisterEncodedLaunchCommand` (`ActivationRegistrationManager.cpp:180-186`) registers an
  in-process COM `IExecuteCommand` handler (`dev/AppLifecycle/EncodedLaunchExecuteCommand.cpp`)
  whose whole job is to resolve an `ms-launch:` URI to the ProgId of the app that declared the
  matching `AppUserModelId` and re-`ShellExecuteEx` it. It exists to let *one app launch another*
  by AUMID through an encoded URI. Nothing in the six public `ActivationRegistrationManager`
  methods reaches it — `RegisterEncodedLaunchSupport`, the only caller, is itself uncalled in the
  reference sources. Skipping it costs Uno Platform nothing and avoids shipping a COM shell
  extension.
- **`FileActivatedEventArgs::Deserialize`'s two-path parsing must be carried over.**
  `dev/AppLifecycle/FileActivatedEventArgs.h:48-91` uses `QueryParsed()` only when it returns
  exactly 3 entries, and hand-parses the query string otherwise, because `QueryParsed()` returns
  empty for a path containing non-ASCII characters and over-splits for a path containing `&`.
  `DecodeActivatedEventArgs` has a matching prefix-comparison fallback for the same reason
  (`ExtensionContract.h:55-61`). A naive `HttpUtility.ParseQueryString` port will lose exactly the
  file paths users complain about.

### 1.6 Proposed Uno Platform shape

One new internal type, shared by all four desktop hosts (all four already reference
`Uno.WinRT.Skia.csproj`, verified in each host `.csproj`):

**`src/Uno.WinRT/Microsoft/Windows/AppLifecycle/CommandLineActivationParser.cs`** *(new)*

```csharp
internal static class CommandLineActivationParser
{
	// Grammar constants, matching dev/AppLifecycle/ActivationRegistrationManager.h:10-14.
	internal const string ArgumentPrefix = "----";
	internal const string ArgumentSuffix = ":";
	internal const string ProtocolMarker = "ms-protocol";
	internal const string PushMarker = "WindowsAppRuntimePushServer";
	internal const string AppNotificationMarker = "AppNotificationActivated";

	internal static bool TryParse(
		string[] arguments,
		out AppActivationArguments? activation,
		out string remainingArguments);
}
```

Contract, mapped one-to-one onto §1.3:

- Iterates `arguments` in order, and for each argument tries the three markers in the documented
  order (P1, §1.4).
- Uses `IndexOf(qualifier, StringComparison.Ordinal)`, not `StartsWith` (P2).
- Advances by `ArgumentPrefix.Length` from the match index, which is 4 (P3) — expressed as the
  constant's length with a comment naming the Windows App SDK's hard-coded `+= 4`, so the two stay
  provably equal.
- Splits on `IndexOf(':')`, not `LastIndexOf` and not `Split(':')` (P4, P5).
- On `ms-protocol`: builds a `ProtocolActivatedEventArgs`; if the URI's scheme is
  `ms-encodedlaunch`, decodes it into the typed args instead (§1.5), falling back to the protocol
  args when the contract id is unknown.
- On the push / app-notification markers: logs `Warning`, returns `false`, and strips the token
  from `remainingArguments`.
- Never throws on malformed input. A `Uri.TryCreate` failure logs and returns `false`, matching
  `AppleUIKitActivation.TryParseUri`
  (`src/Uno.UI.Runtime.Skia.AppleUIKit/AppleUIKitActivation.cs:61-77`).

`remainingArguments` exists so the host can call `Application.SetArguments(remainingArguments)`
before `Application.Start`, exactly as the WebAssembly host does, keeping the transport marker out
of `OnLaunched`'s `LaunchActivatedEventArgs.Arguments`. The consumption side of that override is
`Application.GetCommandLineArgsWithoutExecutable`, whose "non-null, not non-empty" and consume-once
semantics are already in place.

**Not** a `.Win32.cs`/`.X11.cs` split: the parser is pure string work with no platform API, and a
single copy is what guarantees the four hosts cannot drift.

---

## 2. Per-target URL ingestion

Every host follows the same two-step shape the mobile hosts already use:

1. Classify the process arguments once, **before** `Application.Start`, and report the result with
   `AppInstance.GetCurrent().SetOrRaiseActivation(...)`. Because `_hasLaunched` is still `false`,
   `AppInstance` stores it, and `GetActivatedEventArgs()` returns it from inside `OnLaunched`
   (`AppInstance.SetOrRaiseActivation`).
2. Report any *later* activation through the same call. `AppInstance` — not the host — decides that
   it is now a raise rather than a store.

That invariant is the reason no host needs its own "am I already running?" flag, and the reason the
redirection work in §4 can be added without touching any host's ingestion code.

### 2.1 Win32

**Cold start.** `Win32Host.RunLoop` (`src/Uno.UI.Runtime.Skia.Win32/Hosting/Win32Host.cs:165-173`)
schedules `Application.Start` onto the Win32 event loop. Classification must happen in
`Win32Host.Initialize` (`:156-163`) or at the top of `RunLoop`, before that schedule, so the stored
activation is visible to `OnLaunched`.

**While running.** A second launch of the same executable is, by default, a second process — Windows
has no `onNewIntent`. Reaching the living instance therefore *requires* the redirection machinery of
§4; there is no cheaper route. The staging in §4.5 makes this explicit: Phase 2 gives Win32
cold-start activation only, and warm delivery arrives with Phase 4.

There is one narrower Win32-only option worth recording and rejecting: `WM_COPYDATA` to a window
found by class name. It is cheaper than a named pipe, but it requires a live top-level window (so it
races app startup), it cannot carry a completion signal, and it does not generalize to Linux. The
named-mutex + named-pipe seam in §4.4 subsumes it.

**Files**

| File | Change |
|---|---|
| `src/Uno.UI.Runtime.Skia.Win32/Hosting/Win32Host.cs` | classify + report in `Initialize`; register the activation extension |
| `src/Uno.WinRT/Microsoft/Windows/AppLifecycle/CommandLineActivationParser.cs` | new, §1.6 |

### 2.2 macOS

macOS is the one desktop target where the OS *does* deliver a URL into a living process, and it does
so through the AppKit application delegate rather than the command line. A `.app` bundle launched by
LaunchServices for a URL scheme receives `application:openURLs:` — on both cold start (after
`applicationDidFinishLaunching:`) and while running.

**Where it hooks in.** The delegate is `UNOApplicationDelegate`, declared at
`src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOApplication.h:68-77` and implemented at
`src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOApplication.m:286-363`. It is installed
at `UNOApplication.m:131` (`app.delegate = ad = [[UNOApplicationDelegate alloc] init];`). The
existing `applicationDidFinishLaunching:` (`UNOApplication.m:288-297`) is what calls back into
managed code to start the app, via the function-pointer pattern
`uno_get_application_start_callback()()`.

The follow-up adds `- (void)application:(NSApplication *)application openURLs:(NSArray<NSURL *> *)urls`
to that same `@implementation`, forwarding through a new callback registered the same way as the
existing ones (`uno_set_application_can_exit_callback` at `UNOApplication.h:60` is the template:
a `typedef`'d function pointer, a setter, and a getter). Managed side:

| File | Change |
|---|---|
| `src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOApplication.h` | `typedef bool (*application_open_urls_fn_ptr)(const char* url);` + setter/getter; declare `application:openURLs:` on `UNOApplicationDelegate` |
| `src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOApplication.m` | implement `application:openURLs:`, forward each URL |
| `src/Uno.UI.Runtime.Skia.MacOS/Native/NativeUno.cs` | P/Invoke for the setter |
| `src/Uno.UI.Runtime.Skia.MacOS/Hosting/MacSkiaHost.cs` | register the `[UnmanagedCallersOnly]` callback next to the existing registrations; classify the command line in `StartApp` (`:156-180`) before `Application.Start` (`:172`) |
| `src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac.xcodeproj/project.pbxproj` | no new file, so no membership change needed |

**Ordering trap.** `application:openURLs:` for a cold URL launch can arrive *before*
`applicationDidFinishLaunching:` completes, and therefore before `Application.Start` has run and
before `Application.Current` exists. The callback must be safe to invoke at that point:
`AppInstance.GetCurrent()` is backed by a `Lazy<AppInstance>` with no dependency on `Application`,
so reporting into it early is correct by construction — the activation is simply *stored*, and
`NotifyLaunched` is not called until `Application.InvokeOnLaunched` runs. This must be covered by a
manual matrix row, because a regression here shows up as "cold URL launch loses the URL".

**Command line still matters on macOS.** A bundle run from a terminal (`open -a`, or the binary
directly) gets arguments, not `openURLs:`. Both paths report into the same funnel.

**Not** `NSAppleEventManager` / `kInternetEventClass`: `application:openURLs:` supersedes it and is
the documented AppKit surface.

### 2.3 Linux / X11

**Cold start.** `X11ApplicationHost.StartApp`
(`src/Uno.UI.Runtime.Skia.X11/Hosting/X11ApplicationHost.cs:287-298`) calls `Application.Start`;
classification goes in `Initialize` (`:300-307`), which already runs synchronously before the run
loop.

**XDG desktop entry.** Freedesktop protocol activation is a `.desktop` file whose `Exec=` line
carries the same `%u`/`%U` field-code role that `%1` plays on Windows, plus a `MimeType=` listing
`x-scheme-handler/<scheme>`. Registration is §3.2.3. Consumption is just the command line — the
launcher substitutes the URI into `Exec=` and starts the process. So on X11 the *same* parser
handles it, with one addition: an Uno Platform-written `.desktop` file should use the Windows App SDK
argument shape (`Exec=/path/App "----ms-protocol:%u"`) rather than a bare `%u`, so a single
`CommandLineActivationParser` serves Windows and Linux identically. Bare `%u` must **also** be
accepted, because third-party packaging (a distro `.desktop` file, a Flatpak manifest) will write it
— see §3.4.

**D-Bus activation.** The freedesktop `DBusActivatable=true` key makes the session bus start the app
by well-known name and call `org.freedesktop.Application.Open(aay, a{sv})` (or `Activate`) on
`/<object/path>`. This is the Linux equivalent of warm delivery, and it is *strictly better* than a
hand-rolled socket: the bus owns the "already running?" question, so `Open` on a running app is
delivered to that app with no race. Uno Platform already has the whole client stack —
`Tmds.DBus.Protocol` is referenced by the X11 host
(`src/Uno.UI.Runtime.Skia.X11/Uno.UI.Runtime.Skia.X11.csproj:25-26`) and used by four existing
features (`Helpers/Theming/LinuxSystemThemeHelper.cs`, `IME/FcitxInputMethod.cs`,
`IME/IBusInputMethod.cs`, `Storage/Pickers/LinuxFilePickerExtension.cs`) — but only as a *client*.
Serving `org.freedesktop.Application` means owning a bus name and exporting an object, which is new
ground for this codebase. That is why it is Phase 4 material and not Phase 2.

**Files**

| File | Change |
|---|---|
| `src/Uno.UI.Runtime.Skia.X11/Hosting/X11ApplicationHost.cs` | classify + report in `Initialize` |
| `src/Uno.UI.Runtime.Skia.X11/ApplicationModel/Activation/X11ActivationRegistrationExtension.cs` | new — `.desktop` writing, §3.2.3 |
| `src/Uno.UI.Runtime.Skia.X11/dbus-interfaces/org.freedesktop.Application.xml` | new — generated types for the served interface |
| `src/Uno.UI.Runtime.Skia.X11/Uno.UI.Runtime.Skia.X11.csproj` | add the `AdditionalFiles` entry next to the existing ones (`:30-38`) |

### 2.4 Linux FrameBuffer

FrameBuffer has no window system, no desktop database, no session-bus assumption and no shell to
route a URI. `FramebufferHost`
(`src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Hosting/FramebufferHost.cs:213`) calls
`Application.Start` directly.

**Scope: argument classification only.** The host runs `CommandLineActivationParser` so that a
process *started* with `----ms-protocol:` — which happens when the same binary is invoked by a
script, a systemd unit, or a kiosk launcher — still reports a `Protocol` activation and still strips
the marker from `OnLaunched`'s arguments. Everything else is explicitly out of scope:

- `ActivationRegistrationManager` stays `[Uno.NotImplemented]` on this target. There is no
  user-level registry to write to.
- Single-instance redirection stays unimplemented. There is no bus and no display server; a kiosk
  device runs one process by construction.

This is the same "classify, don't register" position WebAssembly holds for a different reason, and
it must be stated in the docs support matrix rather than left as an inferred gap.

| File | Change |
|---|---|
| `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Hosting/FramebufferHost.cs` | classify + report before `Application.Start` |

---

## 3. `ActivationRegistrationManager`

### 3.1 What the Windows App SDK actually does

Three facts, all verified:

**(a) HKCU only — never HKLM.** Every registry operation in `dev/AppLifecycle/Association.cpp` is
rooted at `GetRegistrationRoot()`, which is a one-line function returning `HKEY_CURRENT_USER`
(`Association.cpp:8-11`). Grepping the file for registry calls confirms there is no other root:
`RegCreateKeyEx`/`RegOpenKeyEx`/`RegDeleteTree` at `:78`, `:88`, `:97`, `:109`, `:124`, `:192`,
`:197`, `:211`, `:224`, `:239`, `:242`, `:280` all take `GetRegistrationRoot()`, and the remainder
(`:147`, `:159`, `:257`, `:375`, `:393`, `:415`, `:427`) take a subkey handle derived from it.
`RegisterForStartupActivation` opens `HKEY_CURRENT_USER` directly
(`ActivationRegistrationManager.cpp:94`). **Consequence: registration never needs elevation, and it
is per-user.** An Uno Platform port that writes HKLM would be both a privilege regression and a
behaviour divergence.

**(b) Every association-changing method ends with `SHChangeNotify`.**
`NotifyShellAssocChanged` (`Association.cpp:436-439`) is
`SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, nullptr, nullptr)`, and it is called at the end of
`RegisterForFileTypeActivation` (`ActivationRegistrationManager.cpp:70`),
`RegisterForProtocolActivation` (`:83`), `UnregisterForFileTypeActivation` (`:125`) and
`UnregisterForProtocolActivation` (`:141`). It is *not* called by the two startup methods — those
touch `Run`, not associations. Without it the shell caches the old association and the registration
appears not to work until the next logon.

**(c) Packaged processes are rejected, not silently ignored.** All six public methods open with
`THROW_HR_IF(E_ILLEGAL_METHOD_CALL, IsPackagedProcess())` — `ActivationRegistrationManager.cpp:45`,
`:76`, `:89`, `:111`, `:131`, `:146` — plus the internal `RegisterForProtocolActivationInternal`
at `:161`. A packaged app declares its associations in its manifest; runtime registration would
conflict. `E_ILLEGAL_METHOD_CALL` surfaces in .NET as `COMException`/`InvalidOperationException`
with HRESULT `0x8000000E`.

The full key layout, for the port:

| Purpose | Key / value | Source |
|---|---|---|
| ProgId root | `HKCU\Software\Classes\<progId>` | `Association.cpp:66-70`, `:132-133` |
| Display name | `…\<progId>\Application\ApplicationName` | `Association.cpp:143-153` |
| Logo | `…\<progId>\DefaultIcon\(Default)` | `Association.cpp:155-164` |
| AUMID | `…\<progId>\AppUserModelId` | `Association.cpp:166-172` |
| Verb command | `…\<progId>\shell\<verb>\command\(Default)` | `Association.cpp:249-262` |
| Scheme marker | `HKCU\Software\Classes\<scheme>\URL Protocol` (empty `REG_SZ`), default value `URL:<scheme>` | `Association.cpp:283-310`, prefix at `Association.h:21` |
| File extension | `HKCU\Software\Classes\<.ext>` | `Association.cpp:317-325` |
| Registered app | `HKCU\Software\RegisteredApplications\<appId>` = capability key path | `Association.cpp:188-205`, path at `Association.h:16` |
| Capability key | `HKCU\Software\Microsoft\WindowsAppRuntimeApplications\<appId>\Capabilties` | `Association.cpp:181-185`, paths at `Association.h:14-15` |
| Association map | `…\Capabilties\{UrlAssociations,FileAssociations}\<assoc>` = `<progId>` | `Association.cpp:337-384` |
| Open-with (file only) | `HKCU\Software\Classes\<.ext>\OpenWithProgids\<progId>` | `Association.cpp:387-400` |

Two naming quirks that a faithful port must reproduce **verbatim**, because they are part of the
on-disk layout that Windows App SDK-registered apps already have:

- The capability subkey is spelled **`Capabilties`** — a typo in the Windows App SDK source
  (`dev/AppLifecycle/Association.h:15`, used at `Association.cpp:184` and `:238`). Writing the
  correctly-spelled `Capabilities` produces keys the Windows App SDK's own unregister path cannot
  find.
- The URL subkey is **`UrlAssociations`**, not `URLAssociations`, despite the comments
  (`Association.cpp:346` vs. the comment at `:373`).

`ComputeAppId` is `"App." + %I64x` of `std::hash<std::wstring>` over the lowercased module path
(`Association.cpp:18-48`), and `ComputeProgId` appends `.File` or `.Protocol`
(`Association.cpp:51-64`, suffixes at `Association.h:26-28`). **`std::hash<std::wstring>` is
implementation-defined and not reproducible from .NET**, so byte-identical ProgIds are not
achievable. See §3.5.

### 3.2 Per-target mapping

#### 3.2.1 Win32 — registry

Direct port of §3.1. `Microsoft.Win32.RegistryKey` is sufficient and there is precedent in the
codebase: `src/Uno.WinRT/System/WindowsLauncherExtension.skia.cs:19-80` already reads
`Software\Classes` through `RegistryKey.OpenBaseKey(hive, view)`. `SHChangeNotify` needs a P/Invoke;
the Win32 host already carries a large generated `Windows.Win32` surface (it uses
`Windows.Win32.System.Registry` in `Helpers/Theming/Win32SystemThemeHelperExtension.cs:8`), so it
belongs there rather than in `Uno.WinRT`.

| Method | Behaviour |
|---|---|
| `RegisterForProtocolActivation(scheme, logo, displayName, exePath)` | ProgId + scheme marker + `open` verb command `App.exe "----ms-protocol:%1"` + registered app + `UrlAssociations`, then `SHChangeNotify`. Throws on empty `scheme` (`ActivationRegistrationManager.cpp:77`). |
| `RegisterForFileTypeActivation(fileTypes, logo, displayName, verbs, exePath)` | ProgId + registered app, then per extension: extension key, one verb command per verb carrying an `ms-encodedlaunch` `Windows.File` URI, `OpenWithProgids`, association map; then `SHChangeNotify`. Throws on empty `fileTypes` (`:46`). Note the reference implementation only ever surfaces **one** file per activation (`FileActivatedEventArgs.h:42-45`), because the shell starts one process per item. |
| `RegisterForStartupActivation(taskId, exePath)` | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\<taskId>` = `App.exe "----ms-protocol:ms-encodedlaunch:App/?ContractId=Windows.StartupTask&TaskId=<taskId>"`. Run-key path at `ActivationRegistrationManager.h:15`. No `SHChangeNotify`. |
| `UnregisterForProtocolActivation(scheme, exePath)` | remove association handler + delete ProgId, then `SHChangeNotify` (`:128-142`). |
| `UnregisterForFileTypeActivation(fileTypes, exePath)` | same per extension, then `SHChangeNotify` (`:108-126`). |
| `UnregisterForStartupActivation(taskId)` | `RegDeleteValue` on the `Run` key, tolerating a missing key (`:144-155`). |

A packaged Uno Platform desktop app must throw `InvalidOperationException` from all six, matching
fact (c). Uno Platform has **no packaged-process detection today** — grepping for
`GetCurrentPackageFullName` / `IsPackaged` finds nothing outside test fixtures — so this needs a
small helper (`GetCurrentPackageFullName` from `kernel32`, `APPMODEL_ERROR_NO_PACKAGE` ⇒
unpackaged). Record it as a prerequisite of Phase 3, not an afterthought.

#### 3.2.2 macOS — LaunchServices at runtime, `Info.plist` at build time

macOS splits this in two, and the split is *not* symmetric with Windows:

- **Declaration is build-time.** A URL scheme is declared in the bundle's `Info.plist` under
  `CFBundleURLTypes`; a document type under `CFBundleDocumentTypes`. LaunchServices discovers them
  when it registers the bundle. There is no supported API to add a URL type to a *running* bundle.
  The repo already documents `CFBundleURLTypes` for iOS/tvOS
  (`doc/articles/features/protocol-activation.md`, "Registering custom scheme") and
  `src/SamplesApp/SamplesApp/Platforms/iOS/Info.plist:60` uses it.
- **Choosing the default handler is runtime.** `LSSetDefaultHandlerForURLScheme(scheme, bundleId)`
  (and `LSSetDefaultRoleHandlerForContentType` for documents) sets *which already-declared* handler
  wins. There is no existing LaunchServices usage anywhere in the repo — grepping for
  `LSSetDefaultHandler` / `LaunchServices` / `kLSRoles` across `.cs`/`.m`/`.h` returns nothing — so
  this is new native surface in `UnoNativeMac`.

| Method | macOS behaviour |
|---|---|
| `RegisterForProtocolActivation` | If the scheme is declared in `CFBundleURLTypes`: call `LSSetDefaultHandlerForURLScheme` for the current bundle id, ignoring `logo`/`displayName` (they come from the bundle) and `exePath` (LaunchServices is bundle-scoped, not exe-scoped). If it is **not** declared: log `Warning` naming the missing `Info.plist` key and no-op — this is the failure users will actually hit, so the message must say what to add. |
| `RegisterForFileTypeActivation` | Report unsupported at runtime; document `CFBundleDocumentTypes`. `LSSetDefaultRoleHandlerForContentType` needs a UTI, not an extension, and mapping arbitrary extensions to UTIs at runtime is not reliable. |
| `RegisterForStartupActivation` | `SMAppService.loginItem` (macOS 13+) registers the bundle as a login item. It takes no arguments, so the `TaskId` cannot ride the command line — this is a real contract gap, see §3.3. |
| `Unregister*` | `LSSetDefaultHandlerForURLScheme` has no "unset"; the closest is handing the scheme back to another handler, which is not expressible. Report unsupported for protocol/file; `SMAppService.unregister()` for startup. |

#### 3.2.3 Linux — `.desktop` + `xdg-mime`

Freedesktop registration is a file, not a database call:

```ini
[Desktop Entry]
Type=Application
Name=<displayName>
Icon=<logo>
Exec=<exePath> "----ms-protocol:%u"
Terminal=false
NoDisplay=true
MimeType=x-scheme-handler/<scheme>;
```

written to `$XDG_DATA_HOME/applications/<appId>.desktop` (default `~/.local/share/applications`),
then `update-desktop-database $XDG_DATA_HOME/applications` and
`xdg-mime default <appId>.desktop x-scheme-handler/<scheme>`. `update-desktop-database` is the
Linux analogue of `SHChangeNotify` — without it the association is not picked up.

| Method | Linux behaviour |
|---|---|
| `RegisterForProtocolActivation` | write/merge the entry above; `x-scheme-handler/<scheme>` into `MimeType`; run `update-desktop-database`; `xdg-mime default`. |
| `RegisterForFileTypeActivation` | same entry with real MIME types. Extension → MIME needs `xdg-mime query filetype` on a probe path, or a small built-in table; `.ext` cannot be used directly. Each verb becomes a separate `Actions=` group, which is the closest freedesktop analogue of a Windows shell verb. |
| `RegisterForStartupActivation` | a second `.desktop` file in `$XDG_CONFIG_HOME/autostart/`, with the `TaskId` carried in `Exec=` exactly as on Windows. This is the one target where the startup contract maps cleanly. |
| `Unregister*` | remove the `MimeType`/`Actions` entries, or delete the file when nothing is left; re-run `update-desktop-database`. |

Per-user, no elevation — the same posture as HKCU. `xdg-mime` and `update-desktop-database` may be
absent on a minimal system; treat a missing tool as a logged `Warning` and a partial success (the
file is written, the default is not set), never as a throw.

#### 3.2.4 Linux FrameBuffer — unsupported

Stays `[Uno.NotImplemented]` on all six. §2.4.

#### 3.2.5 Android / iOS / tvOS — manifest-only, so report unsupported

Both platforms accept association declarations **only** at build time: an Android
`[IntentFilter]` on the activity, an iOS `CFBundleURLTypes` entry — both already documented in
`doc/articles/features/protocol-activation.md`. Nothing at runtime can add one. All six methods
must therefore report unsupported rather than silently no-op, so an app that calls them on mobile
learns immediately instead of shipping a scheme that never fires.

Convention: keep `[Uno.NotImplemented]` on those targets, which routes through
`ApiInformation.TryRaiseNotImplemented` and is already the codebase's documented answer for
"this API cannot exist here".

#### 3.2.6 WebAssembly — fold `Uno.Helpers.ProtocolActivation.RegisterCustomScheme` in

WebAssembly is the one target that *has* the capability but exposes it through an Uno Platform-only
API. `Uno.Helpers.ProtocolActivation.RegisterCustomScheme(string scheme, Uri domain, string prompt)`
(`src/Uno.WinRT/Helpers/ProtocolActivation.wasm.cs:49-102`) validates the scheme against the
`navigator.registerProtocolHandler` rules — either one of 23 predefined schemes
(`ProtocolActivation.wasm.cs:16-40`) or a `web+`-prefixed lowercase-ASCII name — appends
`?unoprotocolactivation=` plus the `%s` placeholder to the given domain, and calls
`navigator.registerProtocolHandler` (`:101`).

**Proposal: `ActivationRegistrationManager.RegisterForProtocolActivation` becomes the public API on
WebAssembly, and `RegisterCustomScheme` is removed.**

Parameter mapping:

| `ActivationRegistrationManager` parameter | WebAssembly meaning |
|---|---|
| `scheme` | the scheme, with the same `web+`/predefined validation as today |
| `logo` | ignored — the browser uses the page favicon |
| `displayName` | the `prompt` string shown in the browser's permission prompt |
| `exePath` | ignored — the handler URL is derived from `document.baseURI` |

The one parameter with no home is `domain`, which `RegisterCustomScheme` requires and validates as
absolute (`ProtocolActivation.wasm.cs:83-91`). It is not a real degree of freedom:
`registerProtocolHandler` rejects a URL outside the page's own origin, so the only value that ever
works is the app's own origin. Deriving it from `document.baseURI` is strictly more correct than
asking the caller, and removes the class of bug where a stale hard-coded `http://localhost:55838/`
is shipped to production — which is verbatim what the current doc sample shows
(`doc/articles/features/protocol-activation.md:108-116`).

**This is a further breaking change**, and must be listed as one:

> **Breaking:** `Uno.Helpers.ProtocolActivation.RegisterCustomScheme` is removed. Call
> `Microsoft.Windows.AppLifecycle.ActivationRegistrationManager.RegisterForProtocolActivation`
> instead, which is the WinUI-shaped API and works on Windows and desktop targets too.
>
> ```diff
> -#if __WASM__
> -    Uno.Helpers.ProtocolActivation.RegisterCustomScheme(
> -        "web+myscheme",
> -        new Uri("https://myapp.example.com/"),
> -        "Can we handle web+myscheme links?");
> -#endif
> +ActivationRegistrationManager.RegisterForProtocolActivation(
> +    "web+myscheme",
> +    logo: "",
> +    displayName: "Can we handle web+myscheme links?",
> +    exePath: "");
> ```
>
> The `domain` argument has no replacement: the handler URL is now derived from the page's own
> origin, which is the only origin the browser ever accepted.

`ProtocolActivation.TryParseActivationUri` and the `unoprotocolactivation` query key stay
`internal` and unchanged — they are the WebAssembly host's transport, not public surface.

The remaining five methods report unsupported on WebAssembly: the browser has no file-type
association and no startup task, and `registerProtocolHandler` has no companion
`unregisterProtocolHandler` reachable from script.

### 3.3 Contract gaps that must be documented, not papered over

| Gap | Where | Disposition |
|---|---|---|
| macOS cannot unregister a URL-scheme default | §3.2.2 | Report unsupported; document that the user changes the default handler in System Settings. |
| macOS login items carry no arguments, so `taskId` is lost | §3.2.2 | `StartupTaskActivatedEventArgs.TaskId` cannot round-trip on macOS. Either report unsupported, or register the login item and synthesize a fixed `TaskId`. **Reporting unsupported is the honest answer** and is what this spec recommends; a synthesized id is a silent lie about which task started the app. |
| ProgIds are not byte-identical to the Windows App SDK's | §3.1, §3.5 | Documented divergence. |
| Linux extension → MIME mapping is heuristic | §3.2.3 | Documented; log which MIME type was chosen. |

### 3.4 Interop with third-party registrations

Uno Platform must consume registrations it did not write. Concretely, on Linux the parser must accept
both `Exec=App "----ms-protocol:%u"` (what Uno Platform writes, §2.3) **and** a bare `Exec=App %u`
(what a distro package or Flatpak manifest writes). The rule: after
`CommandLineActivationParser.TryParse` returns `false`, a *single* remaining argument that parses as
an absolute URI with a non-`file` scheme is treated as a protocol activation. This heuristic is
Linux-only and must not be applied on Windows, where a bare URI argument is indistinguishable from a
user-typed argument and the Windows App SDK contract is authoritative.

### 3.5 Explicitly not reproduced

- **`ComputeAppId`'s exact hash.** `std::hash<std::wstring>` is implementation-defined
  (`Association.cpp:32-43`). The port uses a documented stable hash over the lowercased module path
  and accepts that ProgIds differ from the Windows App SDK's. **Impact:** an app that migrates from
  Windows App SDK to Uno Platform and calls `RegisterForProtocolActivation` writes a *new* ProgId
  and leaves the old one orphaned in `HKCU`. Document it, and consider a one-time sweep of
  `HKCU\Software\RegisteredApplications` for the old-shaped entry.
- **The `ms-launch` delegate-execute COM handler.** §1.5.
- **Push and app-notification contracts.** §1.4.

---

## 4. Single-instance redirection

### 4.1 The Windows App SDK design

Reproducing the *contract* requires understanding the mechanism, because the observable semantics
fall out of it. Every named object is scoped by two computed names built in the `AppInstance`
constructor (`dev/AppLifecycle/AppInstance.cpp:104-169`):

- `m_moduleName = ComputeAppId()` — `"App." + hash(lowercased module path)` (`:110`).
- `m_processName = "<moduleName>_<processId>"` (`:111`).

| # | Named object | Name | Purpose | Source |
|---|---|---|---|---|
| 1 | **File mapping** (shared memory) | `<moduleName>_Module` | The PID list — a fixed `DWORD[512]` array of live instance PIDs. | `:113`; `SharedProcessList.h:9-19`; `SharedMemory.h:94-106` |
| 2 | **Mutex, per instance** | `<processName>_Mutex` | The data lock guarding the PID list, the key, and the request queue. Created with `CREATE_MUTEX_INITIAL_OWNER` by the owning process, so it always exists before anyone opens it. | `:119-121` |
| 3 | **Event** (manual reset) | `<processName>_ActivatedEvent` | Signals the target instance that a redirection request is queued. Watched by a threadpool wait. | `:116-117`, `:129-140`; suffix at `AppInstance.h:15` |
| 4 | **File mapping** | `<processName>_Key` | The instance's registration key, as raw `wchar_t`. | `:147`; `SharedMemory<wchar_t> m_key` at `AppInstance.h:76` |
| 5 | **Mutex, per key** | `<moduleName>_<escapedKey>_Mutex` | Key *ownership*. The first process to acquire it owns the key; `\` in the key is replaced with `_`. Held for the lifetime of ownership via a member lock. | `TrySetKey`, `:589-625` |
| 6 | **File mapping** | `<processName>_RedirectionQueue` | A 4096-slot intrusive singly-linked free list of `GUID` request ids, addressed by *offset* rather than pointer because the mapping lands at different base addresses in each process. | `:168`; `RedirectionRequestQueue.h:10-53`, `:102-166` |
| 7 | **File mapping**, per request | `<processName>_RedirectionRequest_<{guid}>` | The marshalled `AppActivationArguments` packet. | Format at `AppInstance.h:14`; `RedirectionRequest.h:10-24` |
| 8 | **Event** (manual reset), per request | `<processName>_RedirectionRequest_<{guid}>_ActivatedEvent` | Cleanup handshake: the sender waits on it so it does not tear down the mapping before the target has opened it. | `:256-258`, `:214-220` |

The request lifecycle, sender side (`QueueRequest`, `:227-272`):

1. Hop to a background thread (`resume_background`) and `CoInitializeEx`.
2. `CoCreateGuid` an id; open file mapping (7) named from it; `MarshalArguments`.
3. Create event (8).
4. `EnqueueRedirectionRequestId(id)` under the data mutex (2) → writes into queue (6).
5. `AllowSetForegroundWindow(targetPid)` — **transfer foreground rights before signalling**.
6. `SetEvent` on (3).
7. `cleanupEvent.wait()` — block until the target confirms it has opened the packet.

Receiver side (`ProcessRedirectionRequests`, `:192-225`), driven by the threadpool watcher on (3):

1. `ResetEvent` on (3).
2. Drain queue (6) under the data mutex until `GUID_NULL`.
3. For each id: open mapping (7), `UnmarshalArguments`, raise `m_activatedEvent(*this, args)`,
   then `SetEvent` on (8) — tolerating a missing event, "it means the waiter gave up" (`:216-220`).

Argument marshalling has two paths (`RedirectionRequest::MarshalArguments`,
`dev/AppLifecycle/RedirectionRequest.cpp:17-76`): if the payload implements
`IInternalValueMarshalable` it is serialized to its `ms-encodedlaunch` URI and copied as a string —
**the same encoding as §1.5**, which is why that URI format is the linchpin of the whole design.
Otherwise it falls back to COM `CoMarshalInterface`/`CoUnmarshalInterface` over an `HGLOBAL` stream.
A leading `bool` marker byte says which (`:43-44`, `:84`).

Instance discovery (`GetInstances`, `:294-342`) reads the PID list under the lock, drops it, then
`OpenProcess(SYNCHRONIZE, …)` on each PID — **removing orphans it cannot open** (`:332-337`). Each
non-current instance also gets a `RegisterWaitForSingleObject` termination watcher that removes it
from the list when its process dies (`:157-166`).

### 4.2 The three semantics apps depend on

These are the observable contract. An Uno Platform implementation may use any transport, but it must
preserve all three or the canonical single-instancing pattern breaks.

**S1 — `RedirectActivationToAsync` on the current instance is a silent no-op.**

```cpp
IAsyncAction AppInstance::RedirectActivationToAsync(AppLifecycle::AppActivationArguments const& args)
{
    if (!m_isCurrent)
    {
        co_await QueueRequest(args);
    }
}
```
(`dev/AppLifecycle/AppInstance.cpp:274-280`.) No throw, no warning. This matters because the
canonical pattern is written to call it unconditionally on whatever `FindOrRegisterForKey` returned:
when *this* process won the key, the call must simply do nothing and let the app carry on. The
already-landed Uno Platform implementation is compatible — it returns a completed no-op
(`AppInstance.RedirectActivationToAsync`) — but for the wrong reason
(there is never another instance). Once redirection is real, the `IsCurrent` guard must become
explicit rather than incidental.

**Decision on the signature (recorded 2026-09-04).** Uno Platform keeps `FindOrRegisterForKey` returning
a non-nullable `AppInstance`, and does *not* pre-emptively annotate it. The Windows App SDK ships no
nullable reference metadata for this surface — Uno Platform's own generated mirror of it
(`src/Uno.WinRT/Generated/3.0.0.0/Microsoft.Windows.AppLifecycle/AppInstance.cs`) carries no `#nullable`
directive and declares the parameter and return type unannotated — so matching upstream means staying
oblivious rather than promising non-null. Returning `null` once redirection lands is then a metadata
change rather than a semantic break, and this phase is no longer gated on shipping inside 7.0.

**S2 — `FindOrRegisterForKey` may return `null`.**

```cpp
if (s_current->TrySetKey(key.c_str())) { return GetCurrent(); }
return s_current->FindForKey(key.c_str());
```
(`:344-357`), and `FindForKey` ends with a bare `return nullptr;` (`:627-639`). This is not
theoretical: `TrySetKey` fails when another process holds the per-key mutex (5), and `FindForKey`
then scans `GetInstances()` comparing `Key()`. The owner can be missing from that scan for real
reasons — it registered the key but has not yet published it into shared memory (the comment at
`:596-597` describes exactly this ordering hazard and mitigates it by taking the data mutex before
creating the key mutex), or it terminated between the two calls.

**Consequence for the C# signature.** The current Uno Platform declaration is
`public static AppInstance FindOrRegisterForKey(string key)` — non-nullable, returning the current
instance unconditionally
(`AppInstance.FindOrRegisterForKey`). Real redirection makes it
`AppInstance?`. **That is a source-breaking change** for callers compiled with
nullable-reference-types enabled, in a file that is `#nullable enable`. It cannot be sneaked in with
the transport; it must be called out and, ideally, landed in the same release as the transport so
apps see one change rather than two.

**S3 — `Key` returns the empty string when unset**, never `null`:

```cpp
if (m_key.IsValid()) { return winrt::hstring(m_key.Get()); }
return winrt::hstring(L"");
```
(`:564-574`.) Uno Platform already matches (`AppInstance.Key`, backed by `_key = string.Empty`).
`UnregisterKey` resets it and releases the key mutex and its lock (`:466-475`); note the deliberate
member *declaration order* in `AppInstance.h:70-76` so the lock destructs before the mutex it locks.

### 4.3 What today's Uno Platform behaviour actually is

| Member | Windows App SDK | Uno Platform today | File |
|---|---|---|---|
| `GetInstances()` | every live PID | single-element list, always the current instance | `AppInstance.cs` |
| `FindOrRegisterForKey(key)` | may return another instance or `null` | always claims the key and returns the current instance | `AppInstance.cs` |
| `RedirectActivationToAsync(args)` | queues to the target when not current | completed no-op | `AppInstance.cs` |
| `IsCurrent` | per-instance | `=> true` | `AppInstance.cs` |
| `ProcessId` | per-instance | `Environment.ProcessId` | `AppInstance.cs` |

So the canonical pattern *compiles and runs* today and behaves as it would on a platform that can
only ever run one instance — correct on Android, iOS and WebAssembly, and on desktop it means a
second launch runs as a second process instead of redirecting.

### 4.4 Proposed cross-platform seam

An internal extension point registered per host, mirroring how the Skia hosts already register
platform behaviour through `ApiExtensibility`:

**`src/Uno.WinRT/Microsoft/Windows/AppLifecycle/ISingleInstanceExtension.cs`** *(new)*

```csharp
internal interface ISingleInstanceExtension
{
	/// <summary>Claims <paramref name="key"/> for this process, or reports who owns it.</summary>
	bool TryClaimKey(string key, out uint ownerProcessId);

	void ReleaseKey();

	/// <summary>Sends an activation to <paramref name="targetProcessId"/>; false when it is gone.</summary>
	Task<bool> SendActivationAsync(uint targetProcessId, AppActivationArguments args);

	/// <summary>Raised when another process redirects an activation to this one.</summary>
	event EventHandler<AppActivationArguments> ActivationReceived;

	IReadOnlyList<uint> GetLiveProcessIds();
}
```

`AppInstance` gains a per-instance `ProcessId`/`IsCurrent`/`Key` (it currently hard-codes all
three), keeps its `Lazy<AppInstance>` current-instance, and adds a private constructor for a remote
instance. Where no extension is registered — WebAssembly, mobile, FrameBuffer, Headless — the
existing single-instance behaviour of §4.3 is kept verbatim. **That is the important property: hosts
that never register the extension see no behaviour change at all.**

Per-target implementations:

| Target | Key ownership | Transport | Notes |
|---|---|---|---|
| **Win32** | named mutex `Local\Uno.AppInstance.<appId>.<escapedKey>`, held for the ownership lifetime | named pipe `\\.\pipe\Uno.AppInstance.<appId>.<pid>`, one message per activation | The mutex reproduces (5) exactly. The pipe replaces (3)+(6)+(7)+(8): a pipe write already carries the packet, the connect already signals, and the read already acknowledges — collapsing four named objects into one and removing the fixed 512-instance and 4096-request ceilings. **`AllowSetForegroundWindow(targetPid)` before writing is not optional** — it is what makes the redirected-to window actually come forward (`:262`), and losing it is the single most likely regression. |
| **Linux / X11** | D-Bus name ownership: `RequestName` on `<appId>.<escapedKey>` with `DBUS_NAME_FLAG_DO_NOT_QUEUE`. Success = owner. | `org.freedesktop.Application.Open(aay, a{sv})` on the owning name | The bus arbitrates ownership atomically, so S2's "owner registered but not yet published" race cannot occur. `Tmds.DBus.Protocol` is already referenced (§2.3); only the *serving* side is new. Falls back to a lock file plus a Unix domain socket in `$XDG_RUNTIME_DIR` when there is no session bus. |
| **macOS** | not needed | not needed | A `.app` bundle is single-instance by construction: LaunchServices re-activates the running bundle and delivers `application:openURLs:` (§2.2). The extension is registered as a *degenerate* implementation: `TryClaimKey` always succeeds, `SendActivationAsync` always returns `false` (there is never another instance to send to), `GetLiveProcessIds` returns just this process. A bare binary launched from a terminal is genuinely a second process, and that is left as documented divergence — the same position AppKit itself takes. |
| **FrameBuffer** | none | none | No extension registered; §4.3 behaviour retained. |
| **Mobile / WebAssembly** | none | none | Single-instance by nature; no extension registered. |

Two details the transport must get right, both learned from the reference implementation:

- **Orphan reaping.** `GetInstances` removes PIDs it cannot `OpenProcess` (`:332-337`). The Win32
  pipe implementation gets this for free — a failed connect means the owner is gone — but it must
  then also release the stale key claim, or the key is permanently unclaimable. On Linux, D-Bus
  handles it: the name is released when the connection drops.
- **The cleanup handshake exists for a reason.** The sender waits on (8) so the packet mapping
  outlives the target's read (`:268-269`). With a named pipe the equivalent is: do not return from
  `SendActivationAsync` until the write is flushed and the peer has read it. Returning early turns a
  redirection into a silently dropped activation under load.

### 4.5 Staging the behaviour change

Adopting real redirection **changes desktop behaviour** for apps that already call
`FindOrRegisterForKey`: today a second launch runs as a second instance; afterwards it redirects into
the first and exits. For an app that has been shipping against today's behaviour, that is a visible
change — one that is almost always the *intended* one, but it must not arrive unannounced.

Staging, in order:

1. **Land the seam, register no host.** `ISingleInstanceExtension`, the per-instance `AppInstance`
   members, the nullable `FindOrRegisterForKey` return. **No observable change on any target** —
   with no extension registered, every code path is the one that runs today. This is the phase that
   carries the source-breaking nullability change, so it lands with release notes and a migration
   note, and nothing else.
2. **Register Win32, opt-in.** A `FeatureConfiguration.AppLifecycle.EnableSingleInstanceRedirection`
   flag, default `false`. Apps that want it get it; existing apps see nothing.
3. **Flip the default to `true`** in the next major, with the flag retained as an escape hatch and
   the change in the breaking-changes list.
4. **X11 after Win32 has a release of real-world exposure.** D-Bus name ownership is
   session-dependent (no bus under a bare X server, different behaviour under Flatpak), so it wants
   the extra bake time.

Explicitly rejected: making redirection unconditional in one step. `FindOrRegisterForKey` is the API
whose semantics an app builds its startup around, and a silent change from "always me" to "maybe
someone else, maybe null" is exactly the class of change that breaks apps between patch versions.

---

## 5. `AppInstance.Restart(string)`

Currently the only `[Uno.NotImplemented]` member left in
`src/Uno.WinRT/Generated/3.0.0.0/Microsoft.Windows.AppLifecycle/AppInstance.cs`. It returns
`Windows.ApplicationModel.Core.AppRestartFailureReason`, which **does** exist in Uno Platform as a
plain enum
(`src/Uno.WinRT/Generated/3.0.0.0/Windows.ApplicationModel.Core/AppRestartFailureReason.cs`:
`RestartPending`, `NotInForeground`, `InvalidUser`, `Other`). No new type is needed.

### 5.1 What the Windows App SDK does

`dev/AppLifecycle/AppInstance.cpp:369-464`:

1. Packaged + app-container ⇒ delegate to `CoreApplication::RequestRestartAsync` (`:380-384`).
   Uno Platform's `CoreApplication.RequestRestartAsync` is itself `[NotImplemented]`
   (`src/Uno.WinRT/Generated/3.0.0.0/Windows.ApplicationModel.Core/CoreApplication.cs:67-70`), so
   this arm has nothing to delegate to and is out of scope.
2. Otherwise (Win32, including Desktop Bridge): create a named mutex
   `<appId>_RequestRestartNowInProgress` with `CREATE_MUTEX_INITIAL_OWNER`; if
   `GetLastError() == ERROR_ALREADY_EXISTS`, return `RestartPending` (`:388-396`). **One restart at
   a time, enforced across the process family.**
3. `DuplicateHandle` the pseudo-handle from `GetCurrentProcess()` into a real, *inheritable* handle
   with `PROCESS_QUERY_LIMITED_INFORMATION | SYNCHRONIZE | PROCESS_TERMINATE` (`:400-404`) — the
   comment explains a pseudo-handle cannot be inherited.
4. Launch `RestartAgent.exe`, resolved as a sibling of the current module
   (`GenerateRestartAgentPath`, `:359-367`; filename constant at `AppInstance.h:16`), with
   `"<agent>" <handleValue> <callerArguments>`, `CREATE_SUSPENDED | EXTENDED_STARTUPINFO_PRESENT`,
   `bInheritHandles = TRUE`, and a `PROC_THREAD_ATTRIBUTE_HANDLE_LIST` naming exactly that one
   handle (`:406-451`). Packaged adds `PROCESS_CREATION_DESKTOP_APP_BREAKAWAY_OVERRIDE` (`:436-443`).
5. `AllowSetForegroundWindow(agentPid)`, `ResumeThread` (`:453-455`).
6. `wil::handle_wait(agentProcess)` — **the API is documented to return only on failure**
   (`:457-460`); on success the agent has already terminated the caller. The trailing
   `return AppRestartFailureReason::Other;` at `:463` is the "we should never get here" path.

The agent (`dev/RestartAgent/main.cpp`) then: reads the inherited handle from `argv[1]`, resolves
the caller's exe path with `QueryFullProcessImageNameW` (`:28`), `TerminateProcess` **and waits for
termination to complete** because it is async (`:31-32`), rebuilds the command line as
`"<callerPath>" <argumentsAfterTheHandleArg>` (`:34-38`), and `CreateProcess`es it suspended,
transfers foreground rights, and resumes (`:63-69`).

### 5.2 Per-target proposal

The separate-agent-process design exists to solve one problem: *something must outlive the process
being killed in order to start its replacement*. Uno Platform needs the same property but should not
ship a second executable — a sibling `.exe` next to the app would have to be packed into every
desktop head and found at runtime, which is a build-system cost out of proportion to the feature.

| Target | Approach | `AppRestartFailureReason` |
|---|---|---|
| **Win32** | Named mutex `Local\Uno.AppInstance.<appId>.RestartPending` for the one-at-a-time guard (verbatim from step 2). Then start the *same* executable with the new arguments plus a `----uno-restart-wait:<pid>` marker, and exit normally. The **new** process waits briefly for the old PID to exit before continuing. This inverts the agent: the replacement waits for the predecessor instead of a third party killing it. It requires no extra binary, and it is strictly safer — a clean `Exit()` runs shutdown, where `TerminateProcess` does not. | `RestartPending` on mutex contention; `Other` on `CreateProcess` failure |
| **macOS** | Same inversion: `Process.Start` the bundle via `open -n -a <bundle> --args …`, then exit. LaunchServices tolerates the overlap. | as above |
| **X11 / FrameBuffer** | Same inversion with `Process.Start(Environment.ProcessPath, args)`, then exit. | as above |
| **Android** | `Intent` with `FLAG_ACTIVITY_CLEAR_TASK \| FLAG_ACTIVITY_NEW_TASK`, then `Process.KillProcess`. Feasible, but out of scope for a desktop spec — record it, do not build it. | — |
| **iOS / tvOS** | No API. Report unsupported. | — |
| **WebAssembly** | `location.reload()` with the new arguments in the query string is the closest analogue. Behaviourally different enough (same process, same page) to warrant `[NotImplemented]` until someone asks. | — |

**Ordering requirement.** `Restart` must not return on success — it must not return at all. The
implementation starts the successor, then requests application exit through the host's existing
`ICoreApplicationExtension` path (`Win32CoreApplicationExtension.Exit`, referenced from
`src/Uno.UI.Runtime.Skia.Win32/Hosting/Win32Host.cs:210-215`). Returning a value while the process
keeps running would make the API mean the opposite of what it means on Windows.

---

## 6. Activation-args types Uno Platform lacks

Both types exist **only as fully-`[NotImplemented]` generated stubs** — verified by reading them,
not inferred:

- `src/Uno.WinRT/Generated/3.0.0.0/Windows.ApplicationModel.Activation/FileActivatedEventArgs.cs` —
  every member throws via `ApiInformation.CreateNotImplementedException`, including `Files`, `Verb`,
  `Kind` and `PreviousExecutionState`. There is no hand-written partial anywhere in `src/`.
- `src/Uno.WinRT/Generated/3.0.0.0/Windows.ApplicationModel.Activation/StartupTaskActivatedEventArgs.cs` —
  same, including `TaskId`.

The interfaces they implement are already generated
(`IFileActivatedEventArgs`, `IFileActivatedEventArgsWithNeighboringFiles`,
`IFileActivatedEventArgsWithCallerPackageFamilyName`, `IStartupTaskActivatedEventArgs`), so the
work is a hand-written partial plus reconciling the generated stub to `#if false`, exactly as the
already-landed `AppInstance` did.

### 6.1 `FileActivatedEventArgs`

Model: `dev/AppLifecycle/FileActivatedEventArgs.h`. Shape to match the existing hand-written
`ProtocolActivatedEventArgs`
(`src/Uno.WinRT/ApplicationModel/Activation/ProtocolActivatedEventArgs.cs`), which is the closest
sibling and already the pattern the branch uses.

**New file:** `src/Uno.WinRT/ApplicationModel/Activation/FileActivatedEventArgs.cs`

| Member | Behaviour | Reference |
|---|---|---|
| `Kind` | `ActivationKind.File` | `FileActivatedEventArgs.h:32`. `ActivationKind.File == 3` (`src/Uno.WinRT/ApplicationModel/Activation/ActivationKind.cs:29`) and `ExtendedActivationKind.File == 3` (`src/Uno.WinRT/Microsoft/Windows/AppLifecycle/ExtendedActivationKind.cs`), so `AppActivationArguments.FromActivatedEventArgs` maps it correctly with no new factory. |
| `Verb` | the shell verb, e.g. `open` | `:122-125` |
| `Files` | `IReadOnlyList<IStorageItem>`, resolved from the path with `StorageFile.GetFileFromPathAsync` | `:44` |
| `PreviousExecutionState` | `NotRunning` for a cold start, `Running` for a redirect — matching what `NativeApplication.TryHandleIntent` already does for protocol (`src/Uno.UI.Runtime.Skia.Android/UI/Xaml/NativeApplication.cs:145-151`) | `ActivatedEventArgsBase.h:33` |
| `NeighboringFilesQuery`, `CallerPackageFamilyName`, `SplashScreen`, `CurrentlyShownApplicationViewId`, `User` | stay `[NotImplemented]` | — |

Two constructor traps carried from the reference:

- **One file per activation.** The reference appends exactly one `StorageFile` (`:42-45`) with the
  comment that the activation mechanism forces a new process per item. Uno Platform should surface a
  list of one and not pretend to support multi-select.
- **Async resolution in a constructor.** `GetFileFromPathAsync(...).get()` blocks (`:44`). Blocking
  during host startup is how you deadlock a single-threaded dispatcher. The Uno Platform version must
  resolve **lazily** — `Files` materializes on first access — the same deferred-`DataProvider` shape
  the macOS drag-and-drop extension uses for its storage items (spec 045 §2.5).

An `internal` constructor taking `(string verb, string path, ApplicationExecutionState)` plus an
`internal static AppActivationArguments CreateFile(FileActivatedEventArgs)` factory on
`AppActivationArguments`, next to the existing `CreateLaunch`/`CreateProtocol`
(`src/Uno.WinRT/Microsoft/Windows/AppLifecycle/AppActivationArguments.cs`).

### 6.2 `StartupTaskActivatedEventArgs`

Model: `dev/AppLifecycle/StartupActivatedEventArgs.h`. Trivial by comparison.

**New file:** `src/Uno.WinRT/ApplicationModel/Activation/StartupTaskActivatedEventArgs.cs`

| Member | Behaviour | Reference |
|---|---|---|
| `Kind` | `ActivationKind.StartupTask` — `1020` on both enums, verified | `StartupActivatedEventArgs.h:20` |
| `TaskId` | the id the task was registered with | `:38-41` |
| `PreviousExecutionState` | `NotRunning` | `ActivatedEventArgsBase.h:33` |
| `SplashScreen`, `User` | stay `[NotImplemented]` | — |

Plus `AppActivationArguments.CreateStartupTask(...)`.

### 6.3 Reconciling the generated stubs

Both generated files must be edited to `#if false` out the members the hand-written partial now
provides, following the pattern already visible in
`src/Uno.WinRT/Generated/3.0.0.0/Microsoft.Windows.AppLifecycle/AppInstance.cs` (where
`GetCurrent`, `Key`, `IsCurrent`, `ProcessId`, `GetActivatedEventArgs`, `UnregisterKey`,
`RedirectActivationToAsync`, `FindOrRegisterForKey`, `GetInstances` and `Activated` are all under
`#if false` while `Restart` stays live). The `Generated/` folder is normally off-limits, but this
narrow reconciliation is the established, sanctioned exception for promoting a stub to a real
implementation.

---

## 7. Delivery plan — phased and danger-ranked

Cheapest and safest first. **Every phase is independently shippable and independently valuable.**

### Phase 1 — Argument classification (lowest risk)

**Danger: very low.** Pure addition. No existing code path changes behaviour unless the process was
started with a `----`-prefixed argument, which nothing does today.

| Deliverable | Files |
|---|---|
| `CommandLineActivationParser` with the full §1.3 grammar, the three markers, and the `ms-encodedlaunch` decode | `src/Uno.WinRT/Microsoft/Windows/AppLifecycle/CommandLineActivationParser.cs` *(new)* |
| `FileActivatedEventArgs` + `StartupTaskActivatedEventArgs` + their `AppActivationArguments` factories, so the decoder has types to produce | `src/Uno.WinRT/ApplicationModel/Activation/FileActivatedEventArgs.cs` *(new)*, `.../StartupTaskActivatedEventArgs.cs` *(new)*, `src/Uno.WinRT/Microsoft/Windows/AppLifecycle/AppActivationArguments.cs`, the two `Generated/` stubs |

**Validation.** Unit tests in `Uno.UI.UnitTests` — the parser is pure string work with no visual
tree, which is exactly what that project is for. Table-driven, one case per row of §1.3 plus:

| Case | Input | Expected |
|---|---|---|
| Colon in data | `----ms-protocol:myscheme:host/path?a=b:c` | data = `myscheme:host/path?a=b:c` (fails on a `LastIndexOf` or `Split` implementation) |
| No colon after kind | `----ms-protocol` | kind = `ms-protocol`, data = `""` |
| Marker not at index 0 | `x----ms-protocol:s:d` | still matches (P2) |
| Marker ordering | both `ms-protocol` and `WindowsAppRuntimePushServer` present | `ms-protocol` wins |
| Encoded file launch | `----ms-protocol:ms-encodedlaunch:App/?ContractId=Windows.File&Verb=open&File=C%3A%5Ca.txt` | `FileActivatedEventArgs`, `Verb == "open"` |
| Encoded startup | `…ContractId=Windows.StartupTask&TaskId=MyTask` | `StartupTaskActivatedEventArgs`, `TaskId == "MyTask"` |
| Non-ASCII file path | a path with non-ASCII characters | resolves through the hand-parse fallback (`FileActivatedEventArgs.h:61-90`) |
| `&` in file path | a path containing `&` | resolves through the same fallback |
| Unknown contract id | `…ContractId=Contoso.Whatever` | falls back to `ProtocolActivatedEventArgs` (`ExtensionContract.h:63`) |
| Push marker | `----WindowsAppRuntimePushServer:…` | not an activation; token stripped from remaining arguments |
| Malformed URI | `----ms-protocol:::` | no activation, no throw |

These are **fails-before/passes-after by construction** — the parser does not exist yet.

### Phase 2 — Cold-start protocol activation per desktop target

**Danger: low.** The only behaviour change to an existing app is that a `----`-prefixed argument
stops appearing in `OnLaunched`'s `Arguments` and shows up as an activation instead. That is the
intended change, and no template generates such an argument today.

| Target | Files |
|---|---|
| Win32 | `src/Uno.UI.Runtime.Skia.Win32/Hosting/Win32Host.cs` |
| X11 | `src/Uno.UI.Runtime.Skia.X11/Hosting/X11ApplicationHost.cs` |
| FrameBuffer | `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Hosting/FramebufferHost.cs` |
| macOS — command line | `src/Uno.UI.Runtime.Skia.MacOS/Hosting/MacSkiaHost.cs` |
| macOS — `application:openURLs:` | `UnoNativeMac/UnoNativeMac/UNOApplication.{h,m}`, `Native/NativeUno.cs`, `Hosting/MacSkiaHost.cs` |
| Docs | `doc/articles/features/protocol-activation.md` — flip the "Skia Desktop (Windows, macOS, Linux)" row from "❌ Not yet" to per-target availability, and add a desktop registration section |

**Validation.** Command-line activation on desktop is launch-time by nature and cannot be reached
from inside a running test host, so it splits:

- **Already runtime-tested:** the funnel's own semantics are covered by
  `src/Uno.UI.RuntimeTests/Tests/Windows_ApplicationModel/Given_AppInstance.cs` — that
  `SetOrRaiseActivation` stores before launch and raises after, that `GetActivatedEventArgs()` keeps
  returning the *launch* activation after a later one is raised, that it is stable across calls, and
  that `FromActivatedEventArgs` maps a protocol payload. **Extend that class rather than starting a
  new one**, and add the cases this phase introduces: a `File` payload mapping to
  `ExtendedActivationKind.File`, and a `StartupTask` payload mapping to `StartupTask`.
  `Given_ProtocolActivation.cs` alongside it covers the WebAssembly query-key parsing and is the
  model for the new `CommandLineActivationParser` cases in Phase 1.
- **Manual, per target**, using the existing SamplesApp reporting hooks
  (`src/SamplesApp/SamplesApp.Shared/App.xaml.cs` already subscribes to `AppInstance.Activated` and
  reports `GetActivatedEventArgs()` from `OnLaunched`):

| # | Scenario | Expected |
|---|---|---|
| 1 | `SamplesApp.exe "----ms-protocol:myscheme:a/b?c=d"` | `GetActivatedEventArgs().Kind == Protocol`; URI intact including the inner colon |
| 2 | Same, with a URI containing `:` in the query | URI not truncated |
| 3 | `SamplesApp.exe --sample=Foo` (plain launch) | `Kind == Launch`; `OnLaunched` sees `--sample=Foo` |
| 4 | `----ms-protocol:` + an `ms-encodedlaunch` file URI | `Kind == File`; `Files[0]` resolves |
| 5 | macOS: `open myscheme://a/b` with the app **not** running | URL reported; window shows it (the §2.2 ordering trap) |
| 6 | macOS: same with the app **already** running | `Activated` fires; `GetActivatedEventArgs()` still returns the original launch |
| 7 | Linux: launch through a hand-written `.desktop` with `Exec=… "----ms-protocol:%u"` | URI reported |
| 8 | Linux: bare `Exec=… %u` | URI reported through the §3.4 heuristic |
| 9 | FrameBuffer: `----ms-protocol:` argument | reported; no registration API attempted |

### Phase 3 — OS registration (`ActivationRegistrationManager`)

**Danger: medium.** This phase *writes to the user's machine* — registry keys, `~/.local/share`
files, LaunchServices defaults. Bugs are persistent and user-visible outside the app, and a bad
unregister can orphan keys. It ships after Phase 2 so the consumption side is already proven: there
is no point registering a scheme the app cannot then read.

| Deliverable | Files |
|---|---|
| Win32 registry implementation + `SHChangeNotify` + packaged detection | `src/Uno.UI.Runtime.Skia.Win32/ApplicationModel/Activation/Win32ActivationRegistrationExtension.cs` *(new)*, `Hosting/Win32Host.cs` |
| macOS LaunchServices + login items | `src/Uno.UI.Runtime.Skia.MacOS/ApplicationModel/Activation/MacOSActivationRegistrationExtension.cs` *(new)*, `UnoNativeMac/UnoNativeMac/UNOApplication.{h,m}`, `Native/NativeUno.cs`, `Hosting/MacSkiaHost.cs` |
| Linux `.desktop` + `xdg-mime` + autostart | `src/Uno.UI.Runtime.Skia.X11/ApplicationModel/Activation/X11ActivationRegistrationExtension.cs` *(new)*, `Hosting/X11ApplicationHost.cs` |
| WebAssembly: fold in `RegisterCustomScheme`; **remove** it | `src/Uno.WinRT/Helpers/ProtocolActivation.wasm.cs`, `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/…`, `doc/articles/features/protocol-activation.md` |
| Promote the six methods out of the generated stub | `src/Uno.WinRT/Generated/3.0.0.0/Microsoft.Windows.AppLifecycle/ActivationRegistrationManager.cs`, plus a new hand-written partial |
| Breaking-change note for `RegisterCustomScheme` | release notes + docs |

**Validation.** Fails-before/passes-after end-to-end per target, because the whole point is a
round trip:

1. Call `RegisterForProtocolActivation("uno-test-<guid>", …)`.
2. Verify the OS artefact directly — `HKCU\Software\Classes\uno-test-…\URL Protocol` exists and the
   verb command matches the §1.5 template; the `.desktop` file exists and `xdg-mime query default`
   returns it; `LSCopyDefaultHandlerForURLScheme` returns the bundle id.
3. Ask the OS to launch the URI (`ShellExecute` / `xdg-open` / `open`) and confirm the app receives
   a `Protocol` activation with the right URI.
4. `UnregisterForProtocolActivation` and confirm the artefact is gone.

Steps 2 and 4 are automatable per target; steps 1 and 3 need a real desktop session. The
GUID-suffixed scheme keeps a failed run from leaving debris behind. **A packaged-process case must
be covered**, or fact (c) will be discovered by a user.

### Phase 4 — Single-instance redirection (highest risk)

**Danger: high.** It changes the semantics of `FindOrRegisterForKey`, introduces cross-process IPC,
and makes `FindOrRegisterForKey` nullable — a source-breaking change. Staged per §4.5.

| Sub-phase | Deliverable | Files |
|---|---|---|
| 4a | The seam; per-instance `AppInstance` members; nullable `FindOrRegisterForKey`. **No host registers it.** | `src/Uno.WinRT/Microsoft/Windows/AppLifecycle/ISingleInstanceExtension.cs` *(new)*, `AppInstance.cs` |
| 4b | Win32 named mutex + named pipe, behind an opt-in feature flag | `src/Uno.UI.Runtime.Skia.Win32/ApplicationModel/Activation/Win32SingleInstanceExtension.cs` *(new)*, `Hosting/Win32Host.cs`, `src/Uno.UI/FeatureConfiguration.cs` |
| 4c | macOS degenerate implementation | `src/Uno.UI.Runtime.Skia.MacOS/ApplicationModel/Activation/MacOSSingleInstanceExtension.cs` *(new)* |
| 4d | X11 D-Bus name ownership + `org.freedesktop.Application` | `src/Uno.UI.Runtime.Skia.X11/ApplicationModel/Activation/X11SingleInstanceExtension.cs` *(new)*, `dbus-interfaces/org.freedesktop.Application.xml` *(new)*, `Uno.UI.Runtime.Skia.X11.csproj` |
| 4e | Flip the default on Win32 | `FeatureConfiguration`, breaking-changes list |

**Validation.** Two levels:

- **Runtime tests (single-process)**, extending `Given_AppInstance.cs`, for everything that does not
  need a second process. S1 (`RedirectActivationToAsync` is a silent no-op on the current instance)
  and S3 (`Key` round-trips through `FindOrRegisterForKey`/`UnregisterKey`) already have coverage
  there, so those become **regression guards** the seam must not break rather than new tests. What is
  net-new for 4a: that `FindOrRegisterForKey`'s declared return type is nullable and that a caller
  handling `null` compiles; that `IsCurrent`/`ProcessId` are read per-instance rather than
  hard-coded; and that with **no** `ISingleInstanceExtension` registered every one of those existing
  tests still passes unchanged — which is the concrete proof of the "no observable change" claim in
  §4.5 step 1.
- **Multi-process integration, manual**, driven by launching a second SamplesApp:

| # | Scenario | Expected |
|---|---|---|
| 1 | Two launches, both `FindOrRegisterForKey("main")` | first owns the key; second gets an instance with `IsCurrent == false` |
| 2 | Second calls `RedirectActivationToAsync` then exits | first raises `Activated` with the second's payload; **its window comes to the foreground** (the `AllowSetForegroundWindow` check) |
| 3 | Owner killed with the key held, then a third launch | third claims the key — proves orphan reaping |
| 4 | 3+ instances redirecting concurrently | every activation is delivered exactly once, none dropped (the cleanup-handshake check) |
| 5 | Owner exits between `TryClaimKey` failing and the send | `SendActivationAsync` returns `false`; caller runs as its own instance rather than hanging (S2) |
| 6 | Key containing `\` | claimed successfully — the reference escapes `\` to `_` in the mutex name (`AppInstance.cpp:591`), and a raw `\` in a mutex name means a namespace |
| 7 | Flag off | behaviour identical to Phase 3 |

### Phase 5 — `Restart`

**Danger: medium.** It terminates the process. Low *blast radius* — nothing calls it today, since it
throws — but a bug means an app that will not come back.

| Deliverable | Files |
|---|---|
| Win32 / macOS / X11 / FrameBuffer restart via the §5.2 inversion, plus the one-at-a-time named mutex | per-host `…/ApplicationModel/Core/` + `src/Uno.WinRT/Microsoft/Windows/AppLifecycle/AppInstance.cs`, `Generated/.../AppInstance.cs` |

**Validation.** Manual only — a test host cannot survive its own restart. Matrix: restart with
arguments and confirm the successor sees them in `OnLaunched`; two concurrent `Restart` calls and
confirm the second returns `RestartPending`; `Restart` while a modal window is open; confirm the
predecessor's shutdown path runs (this is the advantage of the inversion over `TerminateProcess`, so
it must actually be verified).

### Phase ordering rationale

| Phase | Danger | Why here |
|---|:---:|---|
| 1 Parsing | very low | pure addition, unit-testable, unblocks everything |
| 2 Cold start | low | the payoff phase; makes protocol activation work on desktop at all |
| 3 Registration | medium | writes to the user's machine; needs Phase 2 to be verifiable |
| 4 Redirection | high | changes existing API semantics; source-breaking; cross-process IPC |
| 5 Restart | medium | terminates the process, but nothing depends on it |

A reasonable ship boundary: **1+2 in one release, 3 in the next, 4a with 3** (so the nullability
change travels with an already-breaking release), **4b–4e and 5 after**.

---

## 8. Open decisions

| # | Decision | Recommendation |
|---|---|---|
| D1 | Does `FindOrRegisterForKey` become `AppInstance?`? | **Yes.** It is what the contract is (S2), and pretending otherwise pushes a `NullReferenceException` onto the app. Land it in 4a, in a release that is already breaking. |
| D2 | Byte-identical ProgIds with the Windows App SDK? | **No** — `std::hash` is not reproducible (§3.5). Document the divergence and consider a one-time sweep for old-shaped keys. |
| D3 | Port the `ms-launch` delegate-execute COM handler? | **No** (§1.5). Nothing in the public surface reaches it. |
| D4 | macOS startup task with a synthesized `TaskId`? | **No** — report unsupported (§3.3). A fake id is worse than a clear gap. |
| D5 | Named pipe or `WM_COPYDATA` on Win32? | **Named pipe** (§2.1) — no window-race, carries a completion signal, and generalizes. |
| D6 | Redirection on by default? | **Not initially** (§4.5). Opt-in, then default in a major. |
| D7 | Where does the parser live? | `src/Uno.WinRT/Microsoft/Windows/AppLifecycle/`, one copy, no platform suffix (§1.6). All four desktop hosts already reference `Uno.WinRT.Skia`. |
| D8 | Should FrameBuffer classify arguments at all? | **Yes** (§2.4) — a script-launched kiosk binary is a real case, and stripping the marker from `OnLaunched` is the same rule every other target follows. |

---

## 9. Constitution gate check (projected)

| Principle | Projected status |
|---|---|
| I. WinUI API Fidelity | ✅ The whole point: `ActivationRegistrationManager` and `AppInstance` reach their Windows App SDK shapes, including a nullable `FindOrRegisterForKey`. One Uno Platform-only API (`RegisterCustomScheme`) is *removed* in favour of the WinUI-shaped one. |
| II. Cross-Platform Parity | ✅ Desktop joins the targets that support activation. Gaps (FrameBuffer, macOS unregister, mobile manifest-only) are enumerated and reported at runtime, not left silent. |
| III. Test-First Quality Gates | ⚠️ Split. Phase 1 is fully unit-testable and fails-before/passes-after. Phases 2–5 are launch-time and multi-process by nature; each carries the runtime tests it *can* have plus an explicit manual matrix, following the precedent of spec 045 §7.1 and spec 002. |
| IV. Performance & Resource Discipline | ✅ Parsing happens once at startup over a handful of arguments. Redirection adds one named mutex and one lazily-created pipe listener per process. |
| V. Generated Code Boundaries | ⚠️ Three generated stubs get the sanctioned stub-to-`#if false` reconciliation (`ActivationRegistrationManager`, `FileActivatedEventArgs`, `StartupTaskActivatedEventArgs`), plus `AppInstance` for `Restart`. No other `Generated/` edits. |
| VI. Backward Compatibility | ⚠️ Two deliberate breaks, both staged and documented: `FindOrRegisterForKey` becomes nullable (Phase 4a), and `Uno.Helpers.ProtocolActivation.RegisterCustomScheme` is removed (Phase 3). |
| VII. WinUI Implementation Alignment | ✅ The command-line grammar, the parser's five behaviours, the registry layout including its two spelling quirks, and the redirection semantics are all ported from the reference implementation with file:line citations. |

---

## 10. References

### Windows App SDK (public `microsoft/WindowsAppSDK`)

- `dev/AppLifecycle/AppInstance.cpp` — command-line parsing (`:35-84`), encoded-launch decode
  (`:86-97`, `:546-549`), named-object setup (`:104-169`), redirection (`:192-280`),
  `GetActivatedEventArgs` (`:477-562`), `Key` (`:564-574`), `TrySetKey` (`:589-625`), `FindForKey`
  (`:627-639`), `Restart` (`:369-464`)
- `dev/AppLifecycle/AppInstance.h` — named-object name formats (`:14-16`), member ordering (`:70-76`)
- `dev/AppLifecycle/ActivationRegistrationManager.h` — the four grammar constants (`:10-14`), Run key (`:15`)
- `dev/AppLifecycle/ActivationRegistrationManager.cpp` — `GenerateCommandLine` (`:21-28`), the six
  public methods (`:41-155`), packaged rejection (`:45`, `:76`, `:89`, `:111`, `:131`, `:146`),
  encoded-launch registration (`:180-197`)
- `dev/AppLifecycle/Association.cpp` — HKCU root (`:8-11`), `ComputeAppId` (`:18-48`),
  `RegisterProgId` (`:128-174`), `RegisterVerb` (`:249-273`), `RegisterProtocol` (`:283-310`),
  capability subkeys (`:337-354`), `NotifyShellAssocChanged` (`:436-439`)
- `dev/AppLifecycle/Association.h` — key paths and the `Capabilties` typo (`:9-28`)
- `dev/AppLifecycle/ValueMarshaling.h` — the two reserved schemes (`:7-8`),
  `IInternalValueMarshalable` (`:15-18`), `GenerateEncodedLaunchUri` (`:20-26`)
- `dev/AppLifecycle/ExtensionContract.h` — contract factory table (`:23-31`), `IsEncodedLaunch`
  (`:33-36`), `DecodeActivatedEventArgs` (`:38-64`)
- `dev/AppLifecycle/FileActivatedEventArgs.h`, `StartupActivatedEventArgs.h`,
  `LaunchActivatedEventArgs.h`, `ProtocolActivatedEventArgs.h`, `ActivatedEventArgsBase.h`
- `dev/AppLifecycle/SharedMemory.h`, `SharedProcessList.h`, `RedirectionRequestQueue.h`,
  `RedirectionRequest.{h,cpp}` — the redirection plumbing
- `dev/AppLifecycle/EncodedLaunchExecuteCommand.cpp` — the `ms-launch` shell handler (not ported)
- `dev/AppLifecycle/AppLifecycle.idl` — the projected surface
- `dev/RestartAgent/main.cpp` — the restart agent

### Uno Platform

- `src/Uno.WinRT/Microsoft/Windows/AppLifecycle/AppInstance.cs`, `AppActivationArguments.cs`,
  `ExtendedActivationKind.cs`
- `src/Uno.WinRT/Generated/3.0.0.0/Microsoft.Windows.AppLifecycle/ActivationRegistrationManager.cs`,
  `AppInstance.cs`
- `src/Uno.WinRT/Generated/3.0.0.0/Windows.ApplicationModel.Activation/FileActivatedEventArgs.cs`,
  `StartupTaskActivatedEventArgs.cs`
- `src/Uno.WinRT/ApplicationModel/Activation/ProtocolActivatedEventArgs.cs`,
  `LaunchActivatedEventArgs.cs`, `ActivationKind.cs`
- `src/Uno.WinRT/Helpers/ProtocolActivation.wasm.cs`
- `src/Uno.WinRT/System/WindowsLauncherExtension.skia.cs` — registry-access precedent
- `src/Uno.UI/UI/Xaml/Application.cs` — `InvokeOnLaunched`, `GetLaunchArguments`,
  `GetCommandLineArgsWithoutExecutable`, `SetArguments`
- `src/Uno.UI.Runtime.Skia.Win32/Hosting/Win32Host.cs`
- `src/Uno.UI.Runtime.Skia.X11/Hosting/X11ApplicationHost.cs`,
  `Uno.UI.Runtime.Skia.X11.csproj` (D-Bus generator wiring)
- `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Hosting/FramebufferHost.cs`
- `src/Uno.UI.Runtime.Skia.MacOS/Hosting/MacSkiaHost.cs`,
  `UnoNativeMac/UnoNativeMac/UNOApplication.{h,m}`, `Native/NativeUno.cs`
- `src/Uno.UI.Runtime.Skia.Android/UI/Xaml/NativeApplication.cs` — reference ingestion
- `src/Uno.UI.Runtime.Skia.AppleUIKit/AppleUIKitActivation.cs` — reference ingestion
- `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Hosting/WebAssemblyBrowserHost.cs` — reference ingestion
- `src/Uno.UI.RuntimeTests/Tests/Windows_ApplicationModel/Given_AppInstance.cs` — existing coverage of
  the funnel's semantics; the class Phases 2 and 4 extend
- `src/Uno.UI.RuntimeTests/Tests/Windows_ApplicationModel/Given_ProtocolActivation.cs` — the model for
  the Phase 1 parser cases
- `src/SamplesApp/SamplesApp.Shared/App.xaml.cs` — activation reporting used by the manual matrices
- `doc/articles/features/protocol-activation.md` — public docs; already `AppInstance`-shaped, with a
  platform-support table whose "Skia Desktop" row this spec's Phase 2 and Phase 3 flip
- Spec `045-macos-drag-and-drop` §7.1 — precedent for framing environment-bound validation
- Spec `002-x11-error-exit` — precedent for compile + manual A/B validation

### Platform documentation

- Freedesktop Desktop Entry Specification — `Exec` field codes, `MimeType`, `Actions`,
  `DBusActivatable`
- Freedesktop `org.freedesktop.Application` D-Bus interface — `Activate`, `Open`, `ActivateAction`
- Apple AppKit `NSApplicationDelegate.application(_:open:)`; `Info.plist` `CFBundleURLTypes` /
  `CFBundleDocumentTypes`; LaunchServices `LSSetDefaultHandlerForURLScheme`; `SMAppService`
- MDN `Navigator.registerProtocolHandler`
- Win32 `SHChangeNotify`, `AllowSetForegroundWindow`, `CommandLineToArgvW`, named pipes
