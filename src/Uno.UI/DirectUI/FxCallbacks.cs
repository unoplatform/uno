using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Islands;

namespace DirectUI;

internal static class FxCallbacks
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void XamlRoot_OnSizeChanged(XamlIsland xamlRoot) => XamlRoot.OnSizeChangedStatic(xamlIsland);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void XamlRoot_RaiseChanged(XamlRoot xamlRoot) => xamlRoot.RaiseChangedEvent();
}
