using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.DirectUI.Lib;

internal static class FxCallbacks
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void FlyoutBase_OnClosing(FlyoutBase flyoutBase, out bool cancel) => FlyoutBase.OnClosingStatic(flyoutBase, out cancel);
}
