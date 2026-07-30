using System;
using Uno.UI.RemoteControl.Server.AppLaunch;

namespace DevServerCore;

/// <summary>
/// Fallback monitor used when the host does not reference the full ApplicationLaunchMonitor implementation.
/// </summary>
internal sealed class NullApplicationLaunchMonitor : IApplicationLaunchMonitor
{
	public void RegisterLaunch(Guid mvid, string? platform, bool isDebug, string ide, string plugin)
	{
	}

	public bool ReportConnection(Guid mvid, string? platform, bool isDebug)
		=> true;
}