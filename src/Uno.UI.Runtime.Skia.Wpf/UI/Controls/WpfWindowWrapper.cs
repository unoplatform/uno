using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class WpfWindowWrapper : INativeWindowWrapper
{
	private readonly UnoWpfWindow _wpfWindow;

	public WpfWindowWrapper(UnoWpfWindow wpfWindow)
	{
		_wpfWindow = wpfWindow;
		_wpfWindow.SizeChanged += OnSizeChanged;
	}

	private void OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e) => 
		SizeChanged?.Invoke(this, new SizeChangedEventArgs(this, default, new Windows.Foundation.Size(e.NewSize.Width, e.NewSize.Height)));

	public bool Visible => _wpfWindow.IsVisible;

	public event SizeChangedEventHandler SizeChanged;
}
