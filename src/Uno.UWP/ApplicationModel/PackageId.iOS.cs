#if __IOS__
using Foundation;
using System.Reflection;
using SystemVersion = global::System.Version;

namespace Windows.ApplicationModel
{
	public partial class PackageId
	{
		private const string BundleDisplayNameKey = "CFBundleDisplayName";
		private const string BundleIdentifierKey = "CFBundleIdentifier";
		private const string BundleShortVersionKey = "CFBundleShortVersionString";
		private const string BundleVersionKey = "CFBundleVersion";

		public string FamilyName => NSBundle.MainBundle.InfoDictionary[BundleIdentifierKey].ToString();

		public string FullName => $"{NSBundle.MainBundle.InfoDictionary[BundleIdentifierKey].ToString()}_{Version}";

		public string Name => NSBundle.MainBundle.InfoDictionary[BundleDisplayNameKey].ToString();

		/// <summary>
		/// Implementation based on
		/// <see cref="https://stackoverflow.com/questions/7281085/"/>
		/// </summary>
		public PackageVersion Version
		{
			get
			{
				var shortVersion = NSBundle.MainBundle.InfoDictionary[BundleShortVersionKey].ToString();
				var bundleVersion = NSBundle.MainBundle.InfoDictionary[BundleVersionKey].ToString();

				var rawVersion = "";
				//short version is optional
				if (string.IsNullOrEmpty(shortVersion))
				{
					//if not provided, use bundle version only
					rawVersion = bundleVersion;
				}
				else
				{
					rawVersion = shortVersion;
					//attach bundle version if a single number
					if (ushort.TryParse(bundleVersion, out var buildNumber))
					{
						rawVersion += $".{buildNumber}";
					}
				}

				var version = SystemVersion.Parse(rawVersion);
				return new PackageVersion(version);
			}
		}
	}
}
#endif
