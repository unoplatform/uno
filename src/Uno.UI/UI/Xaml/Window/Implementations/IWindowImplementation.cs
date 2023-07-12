using System;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal interface IWindowImplementation
{
	//bool Visible { get; }

	//Rect Bounds { get; }

	//CoreWindow CoreWindow { get; }

	UIElement Content { get; set; }

	//event TypedEventHandler<object, WindowActivatedEventArgs> Activated;

	//event TypedEventHandler<object, EventArgs> Closed;

	//event TypedEventHandler<object, WindowSizeChangedEventArgs> SizeChanged;

	//event TypedEventHandler<object, VisibilityChangedEventArgs> VisibilityChanged;

	//void Activate();

	//void Close();
}
