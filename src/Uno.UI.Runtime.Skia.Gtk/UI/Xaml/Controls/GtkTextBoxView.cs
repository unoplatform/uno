#nullable enable

using Gtk;

namespace Uno.UI.Runtime.Skia.UI.Xaml.Controls;

internal abstract class GtkTextBoxView : ITextBoxView
{
	protected abstract Widget RootWidget { get; }

	protected abstract Widget ActualInputWidget { get; }

	public void AddToTextInputLayer(Fixed layer)
	{
		if (RootWidget.Parent != layer)
		{
			layer.Put(RootWidget, 0, 0);
		}
	}

	public void RemoveFromTextInputLayer()
	{
		if (RootWidget.Parent is Fixed layer)
		{
			layer.Remove(RootWidget);
		}
	}
	
	public void SetFocus(bool isFocused) => ActualInputWidget.HasFocus = isFocused;

	public void SetSize(double width, double height)
	{
		ActualInputWidget.SetSizeRequest((int)width, (int)height)
	}
}
