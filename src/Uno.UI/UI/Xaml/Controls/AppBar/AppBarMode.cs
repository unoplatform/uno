using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls;

internal enum AppBarMode
{
	Floating,
	Top,
	Bottom,
	Inline, // Similar to floating, except it doesn't register with appbarservice.
}
