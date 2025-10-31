using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Uno.UI.RemoteControl.Helpers;

internal static class ApplicationInfoHelper
{
	/// <summary>
	/// Gets the target platform value from the assembly's TargetPlatformAttribute when present; otherwise returns the provided default ("Desktop" by default).
	/// </summary>
	public static string? GetTargetPlatform(Assembly assembly)
		=> assembly.GetCustomAttribute<TargetPlatformAttribute>()?.PlatformName;

	/// <summary>
	/// Returns the MVID (Module Version Id) of the given assembly.
	/// </summary>
	public static Guid GetMvid(Assembly assembly) => assembly.ManifestModule.ModuleVersionId;
}
