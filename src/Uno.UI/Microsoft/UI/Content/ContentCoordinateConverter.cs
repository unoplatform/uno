using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
