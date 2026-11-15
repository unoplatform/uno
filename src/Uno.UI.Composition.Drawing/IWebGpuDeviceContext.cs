#nullable enable

using Silk.NET.WebGPU;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The WebGPU device the host created for the <see cref="GraphicsContextKind.WebGpu"/> kind, as Silk.NET.WebGPU
/// objects, and the colour format it chose.
/// </summary>
public unsafe interface IWebGpuDeviceContext : IGraphicsContext
{
	/// <summary>
	/// The API these objects belong to. Every call on them goes through it: an API loaded separately binds another
	/// WebGPU implementation, which cannot use them.
	/// </summary>
	WebGPU Api { get; }

	/// <summary>The instance, or null in the browser, where JavaScript creates the device.</summary>
	Instance* Instance { get; }

	/// <summary>The adapter, or null in the browser, where JavaScript creates the device.</summary>
	Adapter* Adapter { get; }

	Device* Device { get; }

	Queue* Queue { get; }

	/// <summary>The colour format the backend's pipelines must use.</summary>
	TextureFormat ColorFormat { get; }
}
