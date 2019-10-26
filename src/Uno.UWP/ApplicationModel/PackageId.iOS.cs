#if __IOS__
using Foundation;
using SystemVersion = global::System.Version;

namespace Windows.ApplicationModel
{
	public partial class PackageId
	{
		private const string BundleIdentifierKey = "CFBundleIdentifier";
		private const string BundleShortVersionKey = "CFBundleShortVersionString";
		private const string BundleVersionKey = "CFBundleVersion";

		private const string DefaultVersionString = "0.0";

		public string FamilyName => NSBundle.MainBundle.InfoDictionary[BundleIdentifierKey]?.ToString() ?? string.Empty;

		public string FullName =>
			$"{NSBundle.MainBundle.InfoDictionary[BundleIdentifierKey]?.ToString() ?? string.Empty}_" +
			$"{NSBundle.MainBundle.InfoDictionary[BundleVersionKey]?.ToString() ?? DefaultVersionString}";

		public string Name => NSBundle.MainBundle.InfoDictionary[BundleIdentifierKey].ToString();

		/// <summary>
		/// Implementation based on <see cref="https://developer.apple.com/documentation/bundleresources/information_property_list/cfbundleversion"/>.
		/// </summary>
		public PackageVersion Version
		{
			get
			{
				var shortVersion = NSBundle.MainBundle.InfoDictionary[BundleShortVersionKey]?.ToString() ?? DefaultVersionString;
				var bundleVersion = NSBundle.MainBundle.InfoDictionary[BundleVersionKey]?.ToString() ?? DefaultVersionString;
				// Short version is the user-displayed version, use if possible
				if (SystemVersion.TryParse(shortVersion, out var userVersion))
				{
					return new PackageVersion(userVersion);
				}
				// If user-displayed version is not set, use the actual app version
				if (SystemVersion.TryParse(bundleVersion, out var appVersion))
				{
					return new PackageVersion(appVersion);
				}
				// Fallback to default
				return new PackageVersion();
			}
		}
	}
}
#endif
