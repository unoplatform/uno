#nullable enable

#if __SKIA__

using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Hosting;

/// <summary>
/// Every backend assembly exposes its drawing seams through one public static *Backend entry class (the Skia add-ins
/// extend SkiaBackend), which is both what an app registers explicitly and what the host builder resolves its
/// defaults through, by name. These
/// pin that shape: a renamed or hidden factory would otherwise only show as a silent fallback to the managed engine.
/// </summary>
[TestClass]
public class Given_UnoPlatformHostBuilder_DrawingSeams
{
	private const string Skia = "Uno.UI.Composition.Skia.SkiaBackend, Uno.UI.Composition.Skia";
	private const string WebGpu = "Uno.UI.Composition.WebGpu.WebGpuBackend, Uno.UI.Composition.WebGpu";
	private const string Managed = "Uno.UI.Composition.Managed.ManagedBackend, Uno.UI.Composition.Managed";
	// The add-ins extend SkiaBackend (C# 14 static extensions), each emitted as a static method on its extension class.
	private const string Svg = "Uno.UI.Composition.Skia.SkiaBackendSvgExtensions, Uno.UI.Svg";
	private const string Lottie = "Uno.UI.Composition.Skia.SkiaBackendLottieExtensions, Uno.UI.Lottie";

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public void When_AddIns_Referenced_Then_They_Are_The_Defaults()
	{
		// The SamplesApp desktop head references both add-ins and registers neither, so the host builder resolved them.
		if (Environment.GetEnvironmentVariable("UNO_MANAGED_SVG") is "1" or "true")
		{
			Assert.Inconclusive("UNO_MANAGED_SVG registers the managed SVG engine explicitly.");
			return;
		}

		Assert.AreEqual("Uno.UI.Svg", SvgRenderer.Current?.GetType().Assembly.GetName().Name, "The Svg.Skia add-in should be the default SVG renderer.");
		Assert.AreEqual("Uno.UI.Lottie", LottieRenderer.Current?.GetType().Assembly.GetName().Name, "The Skottie add-in should be the default Lottie renderer.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[DataRow(Skia, "CreateGraphicsProvider", typeof(IGraphicsProvider))]
	[DataRow(Skia, "CreateFontProvider", typeof(IFontProvider))]
	[DataRow(Skia, "CreateImageDecoder", typeof(IImageEncoderDecoder))]
	[DataRow(Skia, "CreateGeometryFactory", typeof(IGeometryFactory))]
	[DataRow(WebGpu, "CreateGraphicsProvider", typeof(IGraphicsProvider))]
	[DataRow(Managed, "CreateGeometryFactory", typeof(IGeometryFactory))]
	[DataRow(Managed, "CreateFontProvider", typeof(IFontProvider))]
	[DataRow(Managed, "CreateImageDecoder", typeof(IImageEncoderDecoder))]
	[DataRow(Managed, "CreateSvgRenderer", typeof(ISvgRenderer))]
	[DataRow(Managed, "CreateLottieRenderer", typeof(ILottieRenderer))]
	[DataRow(Svg, "CreateSvgRenderer", typeof(ISvgRenderer))]
	[DataRow(Lottie, "CreateLottieRenderer", typeof(ILottieRenderer))]
	public void When_Registering_Explicitly_Then_Entry_Point_Is_Public(string typeName, string method, Type seam)
	{
		// The test assembly references none of these, as an app's shared code might not: reached by name, public only.
		var factory = Type.GetType(typeName, throwOnError: true)!
			.GetMethod(method, BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);

		Assert.IsNotNull(factory, $"{typeName} should expose a public static {method}() for explicit registration.");
		Assert.IsInstanceOfType(factory.Invoke(null, null), seam);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[DataRow(Skia)]
	[DataRow(WebGpu)]
	[DataRow(Managed)]
	[DataRow(Svg)]
	[DataRow(Lottie)]
	public void When_Backend_Assembly_Then_Its_Implementations_Are_Not_Public(string entryTypeName)
	{
		// The entry class is the only way in: no seam implementation in its assembly is public.
		var assembly = Type.GetType(entryTypeName, throwOnError: true)!.Assembly;
		foreach (var type in assembly.GetExportedTypes())
		{
			Assert.IsFalse(
				typeof(IGraphicsProvider).IsAssignableFrom(type) || typeof(IFontProvider).IsAssignableFrom(type)
					|| typeof(IImageEncoderDecoder).IsAssignableFrom(type) || typeof(IGeometryFactory).IsAssignableFrom(type)
					|| typeof(ISvgRenderer).IsAssignableFrom(type) || typeof(ILottieRenderer).IsAssignableFrom(type),
				$"{type.FullName} implements a drawing seam and is public; expose it through the assembly's *Backend factories instead.");
		}
	}
}
#endif
