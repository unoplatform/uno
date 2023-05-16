#if !(__IOS__ || __ANDROID__ || __MACOS__)
using ProcessorArchitecture = Windows.System.ProcessorArchitecture;

namespace Windows.ApplicationModel;

public sealed partial class PackageId
{
	internal PackageId()
	{
	}

	[Uno.NotImplemented]
	public ProcessorArchitecture Architecture => ProcessorArchitecture.Unknown;

	[global::Uno.NotImplemented("__WASM__, __SKIA__")]
	public string FamilyName => "Unknown";

	[global::Uno.NotImplemented("__WASM__, __SKIA__")]
	public string FullName => "Unknown";

	public string Name { get; internal set; } = "";

	public PackageVersion Version { get; internal set; }

	public string Publisher { get; internal set; } = "";

	[Uno.NotImplemented]
	public string PublisherId => "Unknown";

	[Uno.NotImplemented]
	public string ResourceId => "Unknown";

	[Uno.NotImplemented]
	public string Author => "Unknown";

	[Uno.NotImplemented]
	public string ProductId => "Unknown";
}
#endif
