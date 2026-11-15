#nullable enable

using Silk.NET.WebGPU;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Implemented by an <see cref="IDrawingFactory"/> for the <see cref="GraphicsContextKind.WebGpu"/> kind so that
/// WebGPU code outside the backend can render into a texture on the backend's own device, which the backend then
/// draws like any other <see cref="ITexture"/>.
/// </summary>
public unsafe interface IWebGpuTextureInterop
{
	/// <summary>
	/// The backend's API. Make every call on its objects through it: an API loaded separately binds another WebGPU
	/// implementation, which cannot use them.
	/// </summary>
	WebGPU Api { get; }

	/// <summary>The instance, or null in the browser, where JavaScript creates the device.</summary>
	Instance* Instance { get; }

	/// <summary>The adapter, or null in the browser, where JavaScript creates the device.</summary>
	Adapter* Adapter { get; }

	Device* Device { get; }

	/// <summary>The device's queue. Commands submitted to it before a frame is presented are visible to that frame.</summary>
	Queue* Queue { get; }

	/// <summary>The format of the textures <see cref="CreateRenderTexture"/> creates.</summary>
	TextureFormat RenderTextureFormat { get; }

	/// <summary>
	/// Creates a texture that external code renders into (it can be a render attachment, copied to and copied from)
	/// and the backend samples as premultiplied-alpha color. The returned <see cref="ITexture"/> owns the handles:
	/// disposing it releases them once no recorded frame still draws it.
	/// </summary>
	ITexture CreateRenderTexture(int pixelWidth, int pixelHeight, out Texture* texture, out TextureView* textureView);
}
