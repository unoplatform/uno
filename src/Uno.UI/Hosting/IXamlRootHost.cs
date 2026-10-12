#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Hosting;

internal interface IXamlRootHost
{
	UIElement? RootElement { get; }

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
