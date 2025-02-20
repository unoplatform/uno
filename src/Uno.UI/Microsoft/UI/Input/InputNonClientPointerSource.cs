using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;

#pragma warning disable CS0067

namespace Microsoft.UI.Input;

/// <summary>
/// Processes pointer input and window messages in the non-client area of a window.
/// </summary>
public partial class InputNonClientPointerSource
{
	/// <summary>
	/// Retrieves an InputNonClientPointerSource object for the specified window.
	/// </summary>
	/// <param name="windowId">The ID of the specified window.</param>
	/// <returns>The non-client window pointer input source.</returns>
	public static InputNonClientPointerSource GetForWindowId(WindowId windowId)
	{
		// UNO TODO: Port InputNonClientPointerSource properly from WinUI
		return new InputNonClientPointerSource();
	}

	/// <summary>
	/// Gets the partition boundaries for each region of the specified type within the non-client area of a window.
	/// </summary>
	/// <param name="region">The region within the non-client area of a window.</param>
	/// <returns>A collection of region boundaries.</returns>
	public RectInt32[] GetRegionRects(NonClientRegionKind region)
	{
		// UNO TODO: Port InputNonClientPointerSource properly from WinUI
		return Array.Empty<RectInt32>();
	}

	/// <summary>
	/// Occurs when the window has entered a move-size loop.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, EnteredMoveSizeEventArgs> EnteredMoveSize;

	/// <summary>
	/// Occurs when the window is about to enter a move-size loop.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, EnteringMoveSizeEventArgs> EnteringMoveSize;

	/// <summary>
	/// Occurs when the window has exited a move-size loop.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, ExitedMoveSizeEventArgs> ExitedMoveSize;

	/// <summary>
	/// Occurs when the window rect is about to change.
	/// </summary>
	public event TypedEventHandler<InputNonClientPointerSource, WindowRectChangingEventArgs> WindowRectChanging;
}
