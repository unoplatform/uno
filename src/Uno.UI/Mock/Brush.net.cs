using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;
using System.Drawing;

namespace Windows.UI.Xaml.Media
{
	//Android partial for Brush
	public abstract partial class Brush
	{
		internal static IDisposable AssignAndObserveBrush(Brush b, Action<Color> colorSetter)
		{
			return null;
		}

	}
}
