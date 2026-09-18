namespace Windows.ApplicationModel;

public sealed partial class PackageId
{
	internal PackageId() => InitializePlatform();

	partial void InitializePlatform();
}
