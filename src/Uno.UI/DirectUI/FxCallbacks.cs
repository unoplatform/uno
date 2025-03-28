using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Uno.UI.Xaml.Islands;
using Windows.System;

namespace DirectUI;

internal static class FxCallbacks
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void DxamlCore_OnCompositionContentStateChangedForUWP() => DXamlCore.Current.OnCompositionContentStateChangedForUWP();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void XamlIslandRoot_OnSizeChanged(XamlIsland xamlIslandRoot) => XamlIsland.OnSizeChangedStatic(xamlIslandRoot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void XamlRoot_RaiseChanged(XamlRoot xamlRoot) => xamlRoot.RaiseChangedEvent();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void UIElement_RaiseProcessKeyboardAccelerators(
		UIElement pUIElement,
		VirtualKey key,
		VirtualKeyModifiers keyModifiers,
		ref bool pHandled,
		ref bool pHandledShouldNotImpedeTextInput) =>
		UIElement.RaiseProcessKeyboardAcceleratorsStatic(pUIElement, key, keyModifiers, ref pHandled, ref pHandledShouldNotImpedeTextInput);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool KeyboardAccelerator_RaiseKeyboardAcceleratorInvoked(KeyboardAccelerator pNativeAccelerator, DependencyObject pElement) =>
		KeyboardAccelerator.RaiseKeyboardAcceleratorInvoked(pNativeAccelerator, pElement);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void KeyboardAccelerator_SetToolTip(KeyboardAccelerator pNativeAccelerator, DependencyObject pParentControl) =>
		KeyboardAccelerator.SetToolTip(pNativeAccelerator, pParentControl);
}
