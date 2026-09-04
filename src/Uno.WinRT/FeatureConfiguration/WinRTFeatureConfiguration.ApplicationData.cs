#nullable enable

using System;

namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class ApplicationData
	{
#if __CROSSRUNTIME__
		private static string? _temporaryFolderPathOverride;
		private static string? _localCacheFolderPathOverride;
		private static string? _applicationDataPathOverride;

		/// <summary>
		/// Allows overriding the root folder path that the application will use
		/// to store its TempState folder.
		/// </summary>
		/// <remarks>Applies to GTK and WPF only.</remarks>
		public static string? TemporaryFolderPathOverride
		{
			get => _temporaryFolderPathOverride;
			set
			{
				EnsureApplicationDataNotInitialized();
				_temporaryFolderPathOverride = value;
			}
		}

		/// <summary>
		/// Allows overriding the root folder path that the application will use
		/// to store its LocalCache folder.
		/// </summary>
		/// <remarks>Applies to GTK and WPF only.</remarks>
		public static string? LocalCacheFolderPathOverride
		{
			get => _localCacheFolderPathOverride;
			set
			{
				EnsureApplicationDataNotInitialized();
				_localCacheFolderPathOverride = value;
			}
		}

		/// <summary>
		/// Allows overriding the root folder that the application will use
		/// to store its application data folders (LocalFolder, RoamingFolder, etc.).
		/// Only applies to Skia targets.
		/// </summary>
		/// <remarks>Applies to GTK and WPF only.</remarks>
		public static string? ApplicationDataPathOverride
		{
			get => _applicationDataPathOverride;
			set
			{
				EnsureApplicationDataNotInitialized();
				_applicationDataPathOverride = value;
			}
		}

		internal static bool IsApplicationDataInitialized { get; set; }

		private static void EnsureApplicationDataNotInitialized()
		{
			if (IsApplicationDataInitialized)
			{
				throw new InvalidOperationException(
					"The property was set too late in the application lifecycle." +
					"Set it in Program.cs, so it is applied before ApplicationData is initialized.");
			}
		}
#endif
	}
}
