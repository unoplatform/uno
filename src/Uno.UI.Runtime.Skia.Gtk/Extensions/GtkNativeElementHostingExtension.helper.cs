using Gtk;
using GtkApplication = Gtk.Application;

namespace Uno.UI.Runtime.Skia.Gtk;

internal partial class GtkNativeElementHostingExtension : Windows.UI.Xaml.Controls.ContentPresenter.INativeElementHostingExtension
{
	public object CreateSampleComponent(string text)
	{
		var vbox = new VBox(false, 5);

		var label = new Label(text);
		vbox.PackStart(label, false, false, 0);

		var hbox = new HBox(true, 3);

		var button1 = new Button("Button 1");
		var button2 = new Button("Button 2");
		hbox.Add(button1);
		hbox.Add(button2);

		vbox.PackStart(hbox, false, false, 0);

		return vbox;
	}
}
