using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace DirectUI;

internal static class FxCallbacks
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void XamlRoot_RaiseChanged(XamlRoot xamlRoot) => xamlRoot.RaiseChangedEvent();
}
