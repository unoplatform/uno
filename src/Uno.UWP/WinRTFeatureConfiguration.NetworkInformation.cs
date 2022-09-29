#nullable disable

namespace Uno
{
	partial class WinRTFeatureConfiguration
	{
		public static class NetworkInformation
		{
			/// <summary>
			/// Gets or sets the hostname that is used to check for network connectivity.
			/// Used to check internet availability on iOS, macOS and Skia targets.
			/// </summary>
			public static string ReachabilityHostname { get; set; } = "www.example.com";
		}

	}
}
