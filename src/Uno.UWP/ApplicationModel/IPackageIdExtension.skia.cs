using Windows.ApplicationModel;

namespace Uno.ApplicationModel
{
	internal interface IPackageIdExtension
	{
		string FamilyName { get; }

		string FullName { get; }

		string Name { get; }

		PackageVersion Version { get; }
	}
}
