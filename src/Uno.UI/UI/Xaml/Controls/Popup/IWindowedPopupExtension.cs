#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Composition.Drawing;
using Windows.Graphics;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by a desktop host that can show a popup in its own top-level window. A host that registers none keeps
/// every popup inside its window. Coordinates are physical pixels on the virtual screen.
/// </summary>
internal interface IWindowedPopupExtension
{
	/// <summary>Whether popups get windows unless the app's <c>FeatureConfiguration.Popup.UseWindowedPopups</c> says otherwise.</summary>
	bool IsEnabledByDefault { get; }

	/// <summary>Whether popups of this window can get a window of their own, given the window's rendering backend.</summary>
	bool IsSupported(XamlRoot xamlRoot);

	/// <summary>Creates a hidden window owned by the window of <paramref name="xamlRoot"/>; null when its graphics can't be set up.</summary>
	IWindowedPopupSite? TryCreateSite(XamlRoot xamlRoot, IWindowedPopupContent content);

	/// <summary>The screen position of the top-left of the content root of <paramref name="xamlRoot"/>.</summary>
	PointInt32 GetScreenOffset(XamlRoot xamlRoot);

	/// <summary>The monitor nearest to a screen point.</summary>
	bool TryGetMonitorBounds(PointInt32 screenPoint, out RectInt32 monitor, out RectInt32 workArea);

	/// <summary>SPI_GETHANDEDNESS: whether the user is right-handed, which opens a pen's menu to the left of the pen.</summary>
	bool IsRightHanded { get; }
}

/// <summary>
/// A popup's window. It never takes activation or keyboard focus. Pointer input over it reaches the owner window's
/// pointer source in owner client coordinates, so it goes through the owner's input processing.
/// </summary>
internal interface IWindowedPopupSite : global::System.IDisposable
{
	/// <summary>The window is gone, for example because its owner was closed.</summary>
	bool IsClosed { get; }

	bool HasFocus { get; }

	bool HasPointerCapture { get; }

	/// <summary>The window's actual screen rectangle.</summary>
	RectInt32 ScreenBounds { get; }

	bool IsVisible { get; }

	/// <param name="rect">The window, gutter included.</param>
	/// <param name="contentBounds">The part of the window which takes input where the host can't tell from the pixels.</param>
	void MoveAndResize(RectInt32 rect, RectInt32 contentBounds);

	/// <summary>Shows the window as soon as its first frame is ready, so no empty window is ever visible.</summary>
	void Show();

	void Hide();

	void SetPointerCapture();

	void ReleasePointerCapture();

	/// <summary>A new frame is available from <see cref="IWindowedPopupContent.DrawFrame"/>. Any thread.</summary>
	void InvalidateRender();
}

/// <summary>What a <see cref="IWindowedPopupSite"/> shows and forwards to.</summary>
internal interface IWindowedPopupContent
{
	/// <summary>
	/// Presents the latest frame on the presenting thread; false when there is none. Always ends the
	/// <paramref name="swapChain"/>'s frame.
	/// </summary>
	bool DrawFrame(ISwapChain swapChain, IDrawingFactory renderer);

	/// <summary>The owner window moved or was resized, so the popup's window must follow it.</summary>
	void OnOwnerBoundsChanged();

	/// <summary>The window is gone, along with its owner.</summary>
	void OnSiteClosed();

	/// <summary>The element whose automation peer answers for the window.</summary>
	UIElement? AutomationRoot { get; }
}
