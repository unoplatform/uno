#nullable enable
#if !WINUI && !HAS_UNO_WINUI
using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Uno.Diagnostics.UI;

public sealed partial class DiagnosticsOverlay : Control
{
	// Note: This file is only to let the DiagnosticsOverlay.xaml (ref unconditionally in Generic.xaml) to compile properly.
}
#endif
