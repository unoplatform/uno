#nullable enable

using System;
using WinUI = Windows.UI.Xaml;
using WpfWindow = System.Windows.Window;

namespace Uno.UI.Skia.Wpf;

internal class UnoWpfWindow : WpfWindow
{
	public UnoWpfWindow(WinUI.Window window)
	{
		window.Shown += OnShown;
		Content = new UnoWpfWindowHost(this, window);
	}

	private void OnShown(object? sender, EventArgs e) => Show();
}
