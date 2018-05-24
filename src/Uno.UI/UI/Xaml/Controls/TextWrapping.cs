using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml
{
	public enum TextWrapping
	{
		NoWrap,
		Wrap,
		WrapWholeWords,
		// Not supported for now on iOS/Android
		WordEllipsis,
	}
}
