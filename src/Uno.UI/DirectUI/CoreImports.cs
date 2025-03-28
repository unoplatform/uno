using System.Runtime.CompilerServices;
using Microsoft.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.System;

namespace DirectUI;

internal static class CoreImports
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static VirtualKeyModifiers Input_GetKeyboardModifiers() => PlatformHelpers.GetKeyboardModifiers();
}
