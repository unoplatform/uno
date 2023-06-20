using System.Reflection;
using Uno.Extensions;
using ProcessorArchitecture = Windows.System.ProcessorArchitecture;

namespace Windows.ApplicationModel;

partial class PackageId
{
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public ProcessorArchitecture Architecture => ProcessorArchitecture.Unknown;

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public string PublisherId => "Unknown";

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public string ResourceId => "Unknown";

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public string Author => "Unknown";

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public string ProductId => "Unknown";

#if !__ANDROID__ && __IOS__ && !__MACOS__ && !__SKIA__ && !__WASM__
	[global::Uno.NotImplemented("IS_UNIT_TESTS")]
	public string FamilyName => "Unknown";

	[global::Uno.NotImplemented("IS_UNIT_TESTS")]
	public string FullName => "Unknown";

	[global::Uno.NotImplemented("IS_UNIT_TESTS")]
	public string Name { get; internal set; } = "Unknown";

	[global::Uno.NotImplemented("IS_UNIT_TESTS")]
	public PackageVersion Version => new PackageVersion(Assembly.GetExecutingAssembly().GetVersionNumber());
#endif

#if !__WASM__ && !__SKIA__ && !__NETSTD_REFERENCE__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__MACOS__")]
	public string Publisher => "Unknown";
#endif
}
