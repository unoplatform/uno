﻿#nullable enable

using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.UI.Core;

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

namespace Uno.UI.Xaml.Controls;

internal interface IWindowImplementation
{
	INativeWindowWrapper? NativeWindowWrapper { get; }

	bool Visible { get; }

	string Title { get; set; }

	XamlRoot? XamlRoot { get; }

	Rect Bounds { get; }

	CoreWindow? CoreWindow { get; }

	UIElement? Content { get; set; }

	object? NativeWindow { get; }

	event WindowActivatedEventHandler? Activated;

	event TypedEventHandler<object, WindowEventArgs>? Closed;

	event WindowSizeChangedEventHandler? SizeChanged;

	event WindowVisibilityChangedEventHandler? VisibilityChanged;

	void Initialize();

	void Activate();

	bool Close();

	void NotifyContentLoaded();
}
