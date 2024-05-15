#nullable enable

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.System;
using Uno.Extensions.System;

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

			return Task.FromResult(process.Start());
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
