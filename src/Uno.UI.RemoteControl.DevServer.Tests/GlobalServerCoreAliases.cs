extern alias RemoteServerCore;

// Provide global aliases for types that now live in ServerCore but need to be referenced
// from files compiled into this test project (including linked sources).

global using ServerCoreIApplicationLaunchMonitor = RemoteServerCore::Uno.UI.RemoteControl.Server.AppLaunch.IApplicationLaunchMonitor;

// Re-expose the interface under its original simple name so linked sources such as
// ApplicationLaunchMonitor.cs can compile without modification.
global using IApplicationLaunchMonitor = RemoteServerCore::Uno.UI.RemoteControl.Server.AppLaunch.IApplicationLaunchMonitor;
