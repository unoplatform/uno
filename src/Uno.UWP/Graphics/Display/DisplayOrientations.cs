using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Graphics.Display
{ 
	[Flags]
    public enum DisplayOrientations
    {
		None = 0,
		Landscape = 1,
		Portrait = 2,
		LandscapeFlipped = 4,
		PortraitFlipped = 8,
    }
}
