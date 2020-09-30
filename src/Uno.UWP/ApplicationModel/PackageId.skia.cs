#nullable enable

using System.Reflection;
using Uno.ApplicationModel;
using Uno.Extensions;
using Uno.Foundation.Extensibility;

namespace Windows.ApplicationModel
{
	public partial class PackageId
	{
		private IPackageIdExtension? _packageIdExtension;

		partial void InitializePlatform()
		{
			ApiExtensibility.CreateInstance(typeof(PackageId), out _packageIdExtension);
		}

		public string FamilyName => _packageIdExtension?.FamilyName ?? "Unknown";

		public string FullName => _packageIdExtension?.FullName ?? "Unknown";

		public string Name => _packageIdExtension?.Name ?? "Unknown";

		public PackageVersion Version => _packageIdExtension?.Version ??
			new PackageVersion(Assembly.GetExecutingAssembly().GetVersionNumber());
	}
}
