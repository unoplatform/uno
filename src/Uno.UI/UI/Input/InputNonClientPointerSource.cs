using System;
using System.Collections.Concurrent;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.Graphics;
#pragma warning disable CS0067
namespace Microsoft.UI.Input;

/// <summary>
/// Represents a source of pointer input that occurs in a window's non-client area
/// (for example the caption, title bar buttons, and resize borders). Provides
/// methods to configure non-client region rectangles and events to receive
/// non-client pointer and window-state notifications.
/// </summary>
public partial class InputNonClientPointerSource
{
	private static ConcurrentDictionary<WindowId, InputNonClientPointerSource> _inputSources = new();
	private readonly AppWindow _appWindow;

	private InputNonClientPointerSource(AppWindow appWindow)
	{
		_appWindow = appWindow;
	}

	internal static void CreateForWindow(AppWindow appWindow)
	{
		var inputSource = new InputNonClientPointerSource(appWindow);
		_inputSources[appWindow.Id] = inputSource;
	}

	/// <summary>
	/// Gets the <see cref="Microsoft.UI.Dispatching.DispatcherQueue"/> associated with the input source.
	/// Use this dispatcher to marshal event handlers or other work to the appropriate UI thread.
	/// </summary>
	public DispatcherQueue DispatcherQueue => _appWindow.DispatcherQueue;

	/// <summary>
	/// Removes any region rectangles previously registered for the specified non-client region kind.
	/// After clearing, input for that region will no longer be treated as part of the specified non-client region.
	/// </summary>
	/// <param name="region">The non-client region kind to clear.</param>
	public void ClearRegionRects(NonClientRegionKind region)
	{
	}

	/// <summary>
	/// Gets the set of rectangle bounds that define the specified non-client region for the window.
	/// These rectangles are in window coordinates and can be used to determine where non-client input should be routed.
	/// </summary>
	/// <param name="region">The non-client region kind to query.</param>
	/// <returns>An array of <see cref="Windows.Graphics.RectInt32"/> describing the region, or an empty array if none are registered.</returns>
	public RectInt32[] GetRegionRects(NonClientRegionKind region)
	{
		return Array.Empty<RectInt32>();
	}

	/// <summary>
	/// Sets the set of rectangle bounds that define the specified non-client region for the window.
	/// These rectangles are expressed in window coordinates and replace any previously registered rectangles for the region.
	/// </summary>
	/// <param name="region">The non-client region kind to set.</param>
	/// <param name="rects">An array of <see cref="Windows.Graphics.RectInt32"/> specifying the rectangles for the region.</param>
	public void SetRegionRects(NonClientRegionKind region, RectInt32[] rects)
	{
	}

	/// <summary>
	/// Removes any region rectangles previously registered for all non-client region kinds.
	/// After calling this method, no non-client region rectangles will be registered for the window.
	/// </summary>
	public void ClearAllRegionRects()
	{
	}

	/// <summary>
	/// Gets the <see cref="Microsoft.UI.Input.InputNonClientPointerSource"/> associated with the specified window identifier.
	/// Use this to obtain or create the non-client input source for a particular window.
	/// </summary>
	/// <param name="windowId">The identifier of the window.</param>
	/// <returns>The input source for the specified window.</returns>
	public static InputNonClientPointerSource GetForWindowId(WindowId windowId)
	{
		if (!_inputSources.TryGetValue(windowId, out var inputSource))
		{
			throw new InvalidOperationException("Window was not found");
		}

		return inputSource;
	}

	/// <summary>
	/// Occurs when the user taps the window caption (for example, double-tap to maximize).
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, NonClientCaptionTappedEventArgs> CaptionTapped;

	/// <summary>
	/// Occurs when a pointer enters a non-client region of the window.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, NonClientPointerEventArgs> PointerEntered;

	/// <summary>
	/// Occurs when a pointer exits a non-client region of the window.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, NonClientPointerEventArgs> PointerExited;

	/// <summary>
	/// Occurs when a pointer moves within a non-client region of the window.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, NonClientPointerEventArgs> PointerMoved;

	/// <summary>
	/// Occurs when a pointer is pressed within a non-client region of the window.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, NonClientPointerEventArgs> PointerPressed;

	/// <summary>
	/// Occurs when a pointer is released within a non-client region of the window.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, NonClientPointerEventArgs> PointerReleased;

	/// <summary>
	/// Occurs when the set of non-client region rectangles registered for the window changes.
	/// Handlers can use this event to update hit-testing or layout that depends on non-client regions.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, NonClientRegionsChangedEventArgs> RegionsChanged;

	/// <summary>
	/// Occurs when the window has entered a move or size operation.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, EnteredMoveSizeEventArgs> EnteredMoveSize;

	/// <summary>
	/// Occurs when the window is entering a move or size operation.
	/// Handlers can use this to prepare state before the operation begins.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, EnteringMoveSizeEventArgs> EnteringMoveSize;

	/// <summary>
	/// Occurs when the window has exited a move or size operation.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, ExitedMoveSizeEventArgs> ExitedMoveSize;

	/// <summary>
	/// Occurs when the window rectangle has changed.
	/// Use this to respond to changes in the window's bounds.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, WindowRectChangedEventArgs> WindowRectChanged;

	/// <summary>
	/// Occurs when the window rectangle is changing.
	/// Handlers can observe or cancel changes while the window is being resized or moved.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, WindowRectChangingEventArgs> WindowRectChanging;
}
