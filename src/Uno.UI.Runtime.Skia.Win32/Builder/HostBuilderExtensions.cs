using System;
using Uno.UI.Runtime.Skia.Win32;

namespace Uno.UI.Hosting;

public static class HostBuilderExtensions
{
	public static IUnoPlatformHostBuilder UseWin32(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new Win32HostBuilder());
		RegisterPasswordVault();
		return builder;
	}

	public static IUnoPlatformHostBuilder UseWin32(this IUnoPlatformHostBuilder builder, Action<Win32HostBuilder> action)
	{
		// Eager: AddHostBuilder defers the callback, so a null would only fault when the host is built.
		ArgumentNullException.ThrowIfNull(action);

		builder.AddHostBuilder(() =>
		{
			var win32Builder = new Win32HostBuilder();
			RegisterPasswordVault();
			if (((IPlatformHostBuilder)win32Builder).IsSupported)
			{
				action.Invoke(win32Builder);
			}
			return win32Builder;
		});

		return builder;
	}

	private static void RegisterPasswordVault()
	{
		if (OperatingSystem.IsWindows())
		{
			Win32PasswordVaultExtension.Register();
		}
	}
}
