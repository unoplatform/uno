using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace DirectUI;

internal static class CoreImports
{
	internal static VirtualKeyModifiers Input_GetKeyboardModifiers() => PlatformHelpers.GetKeyboardModifiers();
}
