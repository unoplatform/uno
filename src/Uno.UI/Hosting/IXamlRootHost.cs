#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Hosting;

internal interface IXamlRootHost
{
	UIElement? RootElement { get; }

	/// <summary>
	/// The backend this host negotiated for its window, or null while none is available yet (a backend whose device
	/// import is asynchronous, or a host that does not draw). Read by the window's <c>CompositionTarget</c> when it
	/// first needs a renderer, so the target never records under one backend and presents under another.
	/// </summary>
	IDrawingFactory? Renderer => null;

	void InvalidateRender();

	/// <summary>
	/// The colour the frame is cleared to, when the host's window carries a solid background brush.
	/// Null leaves the frame transparent, which is what a host without a window background wants.
	/// </summary>
	Windows.UI.Color? BackgroundColor => null;

	/// <summary>
	/// Resigns native first responder
	/// </summary>
	void ResignNativeFocus() { }
}
