#nullable enable

using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal interface IWindowImplementation
{
	bool Visible { get; }

	XamlRoot? XamlRoot { get; }

	Rect Bounds { get; }

	CoreWindow? CoreWindow { get; }

	UIElement? Content { get; set; }

	event WindowActivatedEventHandler? Activated;

	event TypedEventHandler<object, WindowEventArgs>? Closed;

	event WindowSizeChangedEventHandler? SizeChanged;

	event WindowVisibilityChangedEventHandler? VisibilityChanged;

	void Activate();

	void Close();
}
