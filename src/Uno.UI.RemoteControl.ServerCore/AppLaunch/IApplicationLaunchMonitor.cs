using System;

namespace Uno.UI.RemoteControl.Server.AppLaunch;

/// <summary>
/// Abstraction over application launch tracking so the devserver core can remain host-agnostic.
/// </summary>
public interface IApplicationLaunchMonitor
{
	/// <summary>
	/// Registers a launch event emitted by the IDE or tooling.
	/// </summary>
	void RegisterLaunch(Guid mvid, string? platform, bool isDebug, string ide, string plugin);

	/// <summary>
	/// Reports that the runtime successfully connected back to the devserver.
	/// </summary>
	bool ReportConnection(Guid mvid, string? platform, bool isDebug);
}