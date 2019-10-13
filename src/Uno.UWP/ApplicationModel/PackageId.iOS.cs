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

		public string FamilyName => NSBundle.MainBundle.InfoDictionary[BundleIdentifierKey].ToString();

		public string FullName => $"{NSBundle.MainBundle.InfoDictionary[BundleIdentifierKey].ToString()}_{NSBundle.MainBundle.InfoDictionary[BundleVersionKey]}";

		public string Name => NSBundle.MainBundle.InfoDictionary[BundleIdentifierKey].ToString();

		/// <summary>
		/// Implementation based on <see cref="https://developer.apple.com/documentation/bundleresources/information_property_list/cfbundleversion"/>.
		/// </summary>
		public PackageVersion Version
		{
			get
			{
				var shortVersion = NSBundle.MainBundle.InfoDictionary[BundleShortVersionKey].ToString();
				var bundleVersion = NSBundle.MainBundle.InfoDictionary[BundleVersionKey].ToString();
				// Short version is the user-displayed version, use if possible
				if ( SystemVersion.TryParse(shortVersion, out var userVersoin))
				{
					return new PackageVersion(userVersoin);
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
