using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Content;
public partial class ContentCoordinateConverter
{
	public static ContentCoordinateConverter CreateForWindowId(WindowId windowId)
	{
		// UNO TODO: Port ContentCoordinateConverter properly from WinUI
		return new ContentCoordinateConverter();
	}
}
