#nullable enable

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal delegate TArgs ChangingFocusEventArgsFactory<TArgs>(
		DependencyObject? oldFocusedElement,
		DependencyObject? newFocusedElement,
		FocusState focusState,
		FocusNavigationDirection direction,
		FocusInputDeviceKind inputDevice,
		bool canCancelFocus,
		Guid correlationId) where TArgs : RoutedEventArgs;

}
