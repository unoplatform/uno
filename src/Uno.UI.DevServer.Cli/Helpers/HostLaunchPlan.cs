namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// How to launch the DevServer host: path to the executable / managed entry-point
/// DLL, and whether the selected TFM is older than the current runtime (in which
/// case the spawn path must set <c>DOTNET_ROLL_FORWARD=Major</c> so the host
/// actually starts).
/// </summary>
/// <param name="HostPath">
/// Absolute path to <c>Uno.UI.RemoteControl.Host.exe</c> (preferred on Windows)
/// or <c>Uno.UI.RemoteControl.Host.dll</c> (used when the <c>.exe</c> shim is
/// absent, e.g. cross-platform builds). Never null.
/// </param>
/// <param name="RequiresMajorRollForward">
/// True when the host was selected via one-major fallback
/// (see <see cref="UnoToolsLocator.TryResolveHostTfm"/>) and the spawned process
/// must therefore honour <c>DOTNET_ROLL_FORWARD=Major</c>. False for exact-TFM
/// matches — setting the flag then would still work but would mask unrelated
/// roll-forward behaviour the user may have pinned via <c>global.json</c> or
/// ambient env vars.
/// </param>
internal sealed record HostLaunchPlan(string HostPath, bool RequiresMajorRollForward);