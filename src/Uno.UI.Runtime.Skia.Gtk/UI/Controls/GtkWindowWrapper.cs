#nullable enable

using System;
using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;

internal class GtkWindowWrapper : INativeWindowWrapper
{
	private readonly UnoGtkWindow _gtkWindow;

	public GtkWindowWrapper(UnoGtkWindow wpfWindow)
	{
		_gtkWindow = wpfWindow ?? throw new ArgumentNullException(nameof(wpfWindow));
		_gtkWindow.SizeAllocated += OnSizeAllocated;
	}

	public UnoGtkWindow NativeWindow => _gtkWindow;

	public bool Visible => _gtkWindow.IsVisible;

	public event SizeChangedEventHandler? SizeChanged;

	public void Activate() => _gtkWindow.Activate();

	private void OnSizeAllocated(object o, global::Gtk.SizeAllocatedArgs args) =>
		SizeChanged?.Invoke(this, new SizeChangedEventArgs(this, default, new Windows.Foundation.Size(args.Allocation.Width, args.Allocation.Height)));
}
