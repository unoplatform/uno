using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Windows.Graphics;

namespace Uno.UI.UI.Input.Internal;

internal interface INativeInputNonClientPointerSource
{
	void ClearAllRegionRects();

	void ClearRegionRects(NonClientRegionKind region);

	RectInt32[] GetRegionRects(NonClientRegionKind region);

	void SetRegionRects(NonClientRegionKind region, RectInt32[] rects);
}
