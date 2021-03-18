#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System.Reflection;
using Uno.Extensions;
using ProcessorArchitecture = Windows.System.ProcessorArchitecture;

namespace Windows.ApplicationModel
{
	public partial class PackageId
	{
		internal PackageId() => InitializePlatform();

		partial void InitializePlatform();

		[Uno.NotImplemented]
		public ProcessorArchitecture Architecture => ProcessorArchitecture.Unknown;

#if !__ANDROID__ && !__IOS__ && !__MACOS__ && !__SKIA__
		[global::Uno.NotImplemented("__WASM__")]
		public string FamilyName => "Unknown";

		[global::Uno.NotImplemented("__WASM__")]
		public string FullName => "Unknown";

		[global::Uno.NotImplemented("__WASM__")]
		public string Name => "Unknown";

		[global::Uno.NotImplemented("__WASM__")]
		public PackageVersion Version => new PackageVersion(Assembly.GetExecutingAssembly().GetVersionNumber());
#endif

		[Uno.NotImplemented]
		public string Publisher => "Unknown";

		[Uno.NotImplemented]
		public string PublisherId => "Unknown";

		[Uno.NotImplemented]
		public string ResourceId => "Unknown";

		[Uno.NotImplemented]
		public string Author => "Unknown";

		[Uno.NotImplemented]
		public string ProductId => "Unknown";
	}
}
