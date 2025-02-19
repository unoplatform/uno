using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;

namespace Microsoft.UI.Content;

/// <summary>
/// Converts between a local coordinate space and the system screen coordinates.
/// </summary>
public partial class ContentCoordinateConverter
{
	/// <summary>
	/// Creates a new ContentCoordinateConverter object for the specified WindowId.
	/// </summary>
	/// <param name="windowId">The top-level window identifier.</param>
	/// <returns>An object used to convert between a local coordinate space and the system screen coordinates.</returns>
	public static ContentCoordinateConverter CreateForWindowId(WindowId windowId)
	{
		// UNO TODO: Port ContentCoordinateConverter properly from WinUI
		return new ContentCoordinateConverter();
	}

	/// <summary>
	/// Converts the local coordinates of the supplied rect to screen coordinate space (adjusted for RasterizationScale).
	/// </summary>
	/// <param name="localRect">The rect to convert from local coordinates to screen coordinate space (adjusted for RasterizationScale).</param>
	/// <returns>The converted rect.</returns>
	public RectInt32 ConvertLocalToScreen(Rect localRect)
	{
		// UNO TODO: Port ContentCoordinateConverter properly from WinUI
		return new RectInt32();
	}

	/// <summary>
	/// Converts the local coordinates of the supplied point collection to screen coordinate space (adjusted for RasterizationScale).
	/// </summary>
	/// <param name="localPoints">The point collection to convert from local coordinates to screen coordinate space (adjusted for RasterizationScale).</param>
	/// <returns>PointInt32[]</returns>
	public PointInt32[] ConvertLocalToScreen(Point[] localPoints)
	{
		// UNO TODO: Port ContentCoordinateConverter properly from WinUI
		return Array.Empty<PointInt32>();
	}

	/// <summary>
	/// Converts the local coordinates of the supplied point collection to screen coordinate space (adjusted for RasterizationScale) using the specified rounding mode.
	/// </summary>
	/// <param name="localPoints">The point collection to convert from local coordinates to screen coordinate space (adjusted for RasterizationScale).</param>
	/// <param name="roundingMode">The rounding mode.</param>
	/// <returns>The converted point collection.</returns>
	public PointInt32[] ConvertLocalToScreen(Point[] localPoints, ContentCoordinateRoundingMode roundingMode)
	{
		// UNO TODO: Port ContentCoordinateConverter properly from WinUI
		return Array.Empty<PointInt32>();
	}

	/// <summary>
	/// Converts the local coordinates of the supplied point to screen coordinate space (adjusted for RasterizationScale).
	/// </summary>
	/// <param name="localPoint">The point to convert from local coordinates to screen coordinate space (adjusted for RasterizationScale).</param>
	/// <returns>The converted point.</returns>
	public PointInt32 ConvertLocalToScreen(Point localPoint)
	{
		// UNO TODO: Port ContentCoordinateConverter properly from WinUI
		return new PointInt32();
	}

	/// <summary>
	/// The point to convert from screen coordinates to local coordinate space (adjusted for RasterizationScale).
	/// </summary>
	/// <param name="screenPoint">The point to convert from screen coordinates to local coordinate space (adjusted for RasterizationScale).</param>
	/// <returns>The converted point.</returns>
	public Point ConvertScreenToLocal(PointInt32 screenPoint)
	{
		// UNO TODO: Port ContentCoordinateConverter properly from WinUI
		return new Point();
	}

	/// <summary>
	/// Converts the screen coordinates of the supplied point collection to local coordinate space (adjusted for RasterizationScale).
	/// </summary>
	/// <param name="screenPoints">The point collection to convert from screen coordinates to local coordinate space (adjusted for RasterizationScale).</param>
	/// <returns>The converted point collection.</returns>
	public Point[] ConvertScreenToLocal(PointInt32[] screenPoints)
	{
		// UNO TODO: Port ContentCoordinateConverter properly from WinUI
		return Array.Empty<Point>();
	}

	/// <summary>
	/// Converts the screen coordinates of the supplied rect to local coordinate space (adjusted for RasterizationScale).
	/// </summary>
	/// <param name="screenRect">The rect to convert from screen coordinates to local coordinate space (adjusted for RasterizationScale).</param>
	/// <returns>The converted rect.</returns>
	public Rect ConvertScreenToLocal(RectInt32 screenRect)
	{
		// UNO TODO: Port ContentCoordinateConverter properly from WinUI
		return new Rect();
	}
}
