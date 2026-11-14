#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.Skia;
using Uno.UI.Hosting;

namespace Uno.UI.Tests.Hosting;

[TestClass]
public class Given_UnoPlatformHostBuilder_SkiaDefaults
{
	[TestMethod]
	public void When_No_Backend_Is_Registered_Then_Skia_Defaults_Are_Allowed()
		=> Assert.IsTrue(UnoPlatformHostBuilder.AllowsSkiaDefaults(Array.Empty<IGraphicsProvider>()));

	[TestMethod]
	public void When_Only_A_Non_Skia_Backend_Is_Registered_Then_Skia_Defaults_Are_Not_Allowed()
		=> Assert.IsFalse(UnoPlatformHostBuilder.AllowsSkiaDefaults(new IGraphicsProvider[] { new NonSkiaProvider() }));

	[TestMethod]
	public void When_A_Skia_Backend_Is_Registered_Then_Skia_Defaults_Are_Allowed()
		=> Assert.IsTrue(UnoPlatformHostBuilder.AllowsSkiaDefaults(new IGraphicsProvider[] { SkiaBackend.CreateGraphicsProvider() }));

	[TestMethod]
	public void When_Skia_Is_The_Fallback_Backend_Then_Skia_Defaults_Are_Allowed()
		=> Assert.IsTrue(UnoPlatformHostBuilder.AllowsSkiaDefaults(new IGraphicsProvider[] { new NonSkiaProvider(), SkiaBackend.CreateGraphicsProvider() }));

	private sealed class NonSkiaProvider : IGraphicsProvider
	{
		public IReadOnlyList<GraphicsContextKind> PreferredContexts { get; } = new[] { GraphicsContextKind.WebGpu };
	}
}
