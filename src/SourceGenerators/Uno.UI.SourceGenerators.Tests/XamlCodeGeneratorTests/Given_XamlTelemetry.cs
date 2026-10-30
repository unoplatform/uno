using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

[TestClass]
public class Given_XamlTelemetry
{
	[TestMethod]
	[DataRow("net10.0-desktop", "TRACE,DEBUG,NET,UNO_REFERENCE_API,HAS_UNO_SKIA,__DESKTOP__,__UNO_SKIA__", "Desktop")]
	[DataRow("net10.0-android", "TRACE,ANDROID,__ANDROID__,UNO_REFERENCE_API,HAS_UNO_SKIA,__UNO_SKIA__", "Android")]
	[DataRow("net10.0-ios", "TRACE,IOS,__IOS__,UNO_REFERENCE_API,HAS_UNO_SKIA,__UNO_SKIA__", "iOS")]
	[DataRow("net10.0-ios18.0", "", "iOS")]
	[DataRow("net10.0-tvos", "TRACE,TVOS,__TVOS__,__IOS__", "tvOS")]
	[DataRow("net10.0-browserwasm", "TRACE,UNO_REFERENCE_API,HAS_UNO_SKIA,__WASM__,__UNO_SKIA__", "WebAssembly")]
	[DataRow("net11.0-desktop", "", "Desktop")]
	[DataRow("net10.0", "TRACE,DEBUG,NET,NET10_0", "Plain")]
	public void When_Classifying_From_TargetFramework(string targetFramework, string defineConstants, string expected)
		=> Assert.AreEqual(expected, XamlCodeGeneration.GetUnoRuntime(targetFramework, defineConstants));

	[TestMethod]
	[DataRow("TRACE,__DESKTOP__", "Desktop")]
	[DataRow("TRACE,DESKTOP", "Desktop")]
	[DataRow("TRACE,__ANDROID__", "Android")]
	[DataRow("TRACE,__IOS__", "iOS")]
	[DataRow("TRACE,__TVOS__,__IOS__", "tvOS")]
	[DataRow("TRACE,__WASM__", "WebAssembly")]
	[DataRow("TRACE,BROWSERWASM", "WebAssembly")]
	[DataRow("TRACE,UNO_REFERENCE_API,__UNO_SKIA__", "Plain")]
	[DataRow("TRACE,__SKIA__,__TIZEN__", "Plain")]
	[DataRow("TRACE,NOT__WASM__SUFFIX", "Plain")]
	public void When_Classifying_From_Symbols(string defineConstants, string expected)
		=> Assert.AreEqual(expected, XamlCodeGeneration.GetUnoRuntime(targetFramework: "", defineConstants));

	[TestMethod]
	public void When_TargetFramework_Platform_Is_Unknown()
		=> Assert.AreEqual("Unknown", XamlCodeGeneration.GetUnoRuntime("net10.0-somethingelse", "__DESKTOP__"));
}
