#nullable enable

using Silk.NET.WebGPU;
using Uno.UI.Composition.Drawing;

namespace Uno.WinUI.Graphics3DWebGpu;

/// <summary>
/// The WebGPU objects a <see cref="WebGpuCanvasElement"/> renders with, as Silk.NET.WebGPU types: the API, the device
/// the app renders with, its queue, and the texture the element displays. The element holds its own references to
/// these objects; don't release them.
/// </summary>
public sealed unsafe class WebGpuCanvasContext
{
	internal WebGpuCanvasContext(IWebGpuTextureInterop interop)
	{
		Api = interop.Api;
		Instance = interop.Instance;
		Adapter = interop.Adapter;
		Device = interop.Device;
		Queue = interop.Queue;
		TargetFormat = interop.RenderTextureFormat;

		if (Instance != null) { Api.InstanceReference(Instance); }
		if (Adapter != null) { Api.AdapterReference(Adapter); }
		Api.DeviceReference(Device);
		Api.QueueReference(Queue);
	}

	/// <summary>The Silk.NET.WebGPU API to render with.</summary>
	public WebGPU Api { get; }

	/// <summary>The WebGPU instance, or null in the browser, where the device is created by JavaScript.</summary>
	public Instance* Instance { get; }

	/// <summary>The WebGPU adapter, or null in the browser, where the device is created by JavaScript.</summary>
	public Adapter* Adapter { get; }

	public Device* Device { get; }

	public Queue* Queue { get; }

	/// <summary>The format of <see cref="Target"/>. Render pipelines that draw into it must use this format.</summary>
	public TextureFormat TargetFormat { get; }

	/// <summary>The texture the element displays. Only valid during <see cref="WebGpuCanvasElement"/>'s RenderOverride.</summary>
	public Texture* Target { get; private set; }

	/// <summary>A view of <see cref="Target"/> to use as a render pass color attachment.</summary>
	public TextureView* TargetView { get; private set; }

	/// <summary>The width of <see cref="Target"/>, in physical pixels.</summary>
	public int PixelWidth { get; private set; }

	/// <summary>The height of <see cref="Target"/>, in physical pixels.</summary>
	public int PixelHeight { get; private set; }

	/// <summary>Creates the backend texture the element displays, and makes it <see cref="Target"/>.</summary>
	internal ITexture CreateTarget(IWebGpuTextureInterop interop, int pixelWidth, int pixelHeight)
	{
		var target = interop.CreateRenderTexture(pixelWidth, pixelHeight, out var texture, out var view);
		ReleaseTarget();
		Target = texture;
		TargetView = view;
		Api.TextureReference(Target);
		Api.TextureViewReference(TargetView);
		PixelWidth = pixelWidth;
		PixelHeight = pixelHeight;
		return target;
	}

	internal void Release()
	{
		ReleaseTarget();
		Api.QueueRelease(Queue);
		Api.DeviceRelease(Device);
		if (Adapter != null) { Api.AdapterRelease(Adapter); }
		if (Instance != null) { Api.InstanceRelease(Instance); }
	}

	private void ReleaseTarget()
	{
		if (TargetView != null) { Api.TextureViewRelease(TargetView); }
		if (Target != null) { Api.TextureRelease(Target); }
		TargetView = null;
		Target = null;
	}
}
