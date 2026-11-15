#if !WINAPPSDK
using Microsoft.UI.Xaml;
using Silk.NET.OpenGL;
using Uno.Foundation.Extensibility;
using Uno.Graphics;
using Uno.WinUI.Graphics3DGL;

[assembly: ApiExtension(typeof(IGLIsland), typeof(GLIsland), typeof(IGLIslandRenderer))]

namespace Uno.WinUI.Graphics3DGL;

/// <summary>
/// The <see cref="IGLIsland"/> extension: a <see cref="GLCanvasElement"/> whose drawing is delegated to an
/// <see cref="IGLIslandRenderer"/>. Lets other add-ins (e.g. Graphics2DSK's GL fallback) render through GL
/// without referencing this assembly.
/// </summary>
public sealed class GLIsland : GLCanvasElement, IGLIsland
{
	private readonly IGLIslandRenderer _renderer;
	private XamlRoot? _observedRoot;
	private double _builtScale = 1;

	public GLIsland(IGLIslandRenderer renderer) : base(null)
	{
		_renderer = renderer;
		Loaded += (_, _) => ObserveRoot(XamlRoot);
		Unloaded += (_, _) => ObserveRoot(null);
	}

	public FrameworkElement Element => this;

	// The island stands in for its owner's own drawing, so it renders at device resolution like the owner would.
	internal override double FramebufferScale => XamlRoot?.RasterizationScale ?? 1;

	protected override void Init(GL gl) => _renderer.Init();

	protected override void OnDestroy(GL gl) => _renderer.Destroy();

	protected override void OnGLUnavailable() => _renderer.OnUnavailable();

	protected override void RenderOverride(GL gl)
	{
		var width = FramebufferPixelWidth;
		var height = FramebufferPixelHeight;
		if (width > 0 && height > 0)
		{
			// The base has already bound our offscreen FBO.
			_renderer.Render((uint)gl.GetInteger(GLEnum.FramebufferBinding), width, height);
		}
	}

	private void ObserveRoot(XamlRoot? root)
	{
		if (_observedRoot is not null)
		{
			_observedRoot.Changed -= OnRootChanged;
		}

		_observedRoot = root;
		if (root is not null)
		{
			root.Changed += OnRootChanged;
			_builtScale = root.RasterizationScale;
		}
	}

	private void OnRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
	{
		if (sender.RasterizationScale != _builtScale)
		{
			_builtScale = sender.RasterizationScale;
			RefreshFramebuffer();
		}
	}
}
#endif
