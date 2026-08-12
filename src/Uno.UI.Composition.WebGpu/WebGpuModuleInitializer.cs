#nullable enable

using System.Runtime.CompilerServices;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// Self-registers the WebGPU context factory (the "GPU-API half" — device + surface creation) into the framework's
/// internal per-kind context registry as soon as this assembly is loaded (i.e. the moment the app constructs a
/// <see cref="WebGpuGraphicsProvider"/>). Context creation is a closed, Uno-owned concern: the app registers only a
/// render backend that announces <see cref="GraphicsContextKind.WebGpu"/>, and the framework creates + hands it a
/// WebGPU context internally (awaiting the async browser device import when needed). The app never wires a factory.
/// </summary>
internal static class WebGpuModuleInitializer
{
	[ModuleInitializer]
	internal static void Initialize()
		=> GraphicsRegistry.RegisterAsyncContextFactory(GraphicsContextKind.WebGpu, WebGpuContextFactory.CreateAsync);
}
