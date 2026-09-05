#nullable enable

using System;

namespace Uno.UI.Runtime.Skia;

internal static class LinuxPasswordVaultExtensions
{
	internal static void Register()
	{
		if (OperatingSystem.IsLinux())
		{
			LinuxPasswordVaultExtension.Register();
		}
	}
}
