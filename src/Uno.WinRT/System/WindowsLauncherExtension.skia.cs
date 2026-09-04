#nullable enable

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.System;
using Uno.Extensions.System;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Extensions.System
{
	internal class WindowsLauncherExtension : ILauncherExtension
	{
		public WindowsLauncherExtension(object owner)
		{
		}

		private const string RegistryPath = @"Software\Classes";

		public Task<bool> LaunchUriAsync(Uri uri)
		{
			var processStartInfo = new ProcessStartInfo(uri.OriginalString)
			{
				UseShellExecute = true,
				Verb = "open"
			};

			var process = new Process()
			{
				StartInfo = processStartInfo
			};

			try
			{
				return Task.FromResult(process.Start());
			}
			catch (Exception ex)
			{
				// WinUI hands the URI to the shell and reports success even when nothing can open it -
				// measured on WinAppSDK 1.7 for ms-resource, ms-appx and unregistered schemes alike.
				// ShellExecute surfaces that failure synchronously here, so report the result WinUI
				// reports rather than letting it escape into HyperlinkButton.OnClick, which is async void.
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning($"Could not launch URI '{uri}' - {ex.Message}");
				}

				return Task.FromResult(true);
			}
		}

		public Task<LaunchQuerySupportStatus> QueryUriSupportAsync(Uri uri, LaunchQuerySupportType launchQuerySupportType)
		{
			var canOpenUri = CheckRegistry(RegistryHive.CurrentUser, uri) || CheckRegistry(RegistryHive.LocalMachine, uri);
			var supportStatus = canOpenUri ?
				LaunchQuerySupportStatus.Available : LaunchQuerySupportStatus.NotSupported;
			return Task.FromResult(supportStatus);
		}

		private static bool CheckRegistry(RegistryHive hive, Uri uri)
		{
			using var key = OpenRegistryKey(hive, RegistryPath, false);

			if (key == null)
			{
				throw new InvalidOperationException(@"Failed to open Registry.");
			}

			using var schemeKey = key.OpenSubKey(uri.Scheme);

			var protocolMark = schemeKey?.GetValue(@"URL Protocol");

			return protocolMark != null;
		}

		private static RegistryKey? OpenRegistryKey(RegistryHive hive, string name, bool writable)
		{
			var view = Environment.Is64BitProcess ? RegistryView.Registry64 : RegistryView.Registry32;
			return RegistryKey.OpenBaseKey(hive, view).OpenSubKey(name, writable);
		}
	}
}
