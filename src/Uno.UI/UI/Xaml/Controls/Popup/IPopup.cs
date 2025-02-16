using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls.Primitives;

internal interface IPopup
{
	event EventHandler<object> Closed;
	event EventHandler<object> Opened;

	bool IsOpen { get; set; }
	UIElement Child { get; set; }
}
