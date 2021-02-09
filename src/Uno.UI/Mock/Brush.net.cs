using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;
using System.Drawing;

namespace Windows.UI.Xaml.Media
{
	public abstract partial class Brush
	{
		internal static IDisposable AssignAndObserveBrush(Brush b, Action<Color> colorSetter, Action imageBrushCallback = null)
		{
			return null;
		}

		// TODO: Refactor brush handling to a cleaner unified approach - https://github.com/unoplatform/uno/issues/5192
		internal bool SupportsAssignAndObserveBrush => true;
	}
}
