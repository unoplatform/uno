#nullable enable

using System;
using System.Linq;
using Gtk;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Runtime.Skia.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Size = Windows.Foundation.Size;
using Point = Windows.Foundation.Point;
using GtkWindow = Gtk.Window;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls;

internal class TextBoxViewExtension : OverlayTextBoxViewExtension
{
	public TextBoxViewExtension(TextBoxView owner)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
	}	
}
