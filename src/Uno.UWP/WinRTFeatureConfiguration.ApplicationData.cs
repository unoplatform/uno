#nullable enable

namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class ApplicationData
	{
#if __SKIA__
		/// <summary>
		/// Allows overriding the root folder path that the application will use
		/// to store its TempState folder.
		/// </summary>
		/// <remarks>Only applies to Skia targets.</remarks>
		public static string? TemporaryFolderPathOverride { get; set; }

		/// <summary>
		/// Allows overriding the root folder that the application will use
		/// to store its application data folders (LocalFolder, RoamingFolder, etc.).
		/// Only applies to Skia targets.
		/// </summary>
		/// <remarks>Only applies to Skia targets.</remarks>
		public static string? ApplicationDataPathOverride { get; set; }
#endif
	}
}
