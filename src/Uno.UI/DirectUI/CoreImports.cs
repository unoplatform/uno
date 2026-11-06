using System.Runtime.CompilerServices;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace DirectUI;

internal static class CoreImports
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static VirtualKeyModifiers Input_GetKeyboardModifiers() => PlatformHelpers.GetKeyboardModifiers();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static short LayoutManager_GetLayoutTickForTransition(Microsoft.UI.Xaml.UIElement target) => Microsoft.UI.Xaml.UIElement.LayoutTick;
}
