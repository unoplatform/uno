#nullable enable

using System;
using System.Linq;
using Gtk;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Xaml.Controls;
using GtkWindow = Gtk.Window;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls
{
	internal class GtkTextBoxViewExtension : ITextBoxViewExtension
	{
		private readonly TextBoxView _owner;
		private readonly GtkWindow _window;
		private Entry? _gtkEntry;

		public GtkTextBoxViewExtension(TextBoxView owner, GtkWindow window)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
			_window = window ?? throw new ArgumentNullException(nameof(window));
		}

		private Fixed GetWindowTextInputLayer()
		{
			var overlay = (Overlay)_window.Child;
			return overlay.Children.OfType<Fixed>().First();
		}

		public void StartEntry()
		{
			_gtkEntry ??= new Entry();
			_gtkEntry.WidthRequest = 120;
			_gtkEntry.HeightRequest = 40;
			_gtkEntry.ShadowType = ShadowType.None;
			_gtkEntry.Text = _owner.DisplayBlock.Text;
			var textInputLayer = GetWindowTextInputLayer();
			textInputLayer.Put(_gtkEntry, 0, 0);
			textInputLayer.ShowAll();
			_gtkEntry.HasFocus = true;
		}

		public void EndEntry()
		{
			if (_gtkEntry == null)
			{
				return;
			}

			_owner.UpdateText(_gtkEntry.Text);

			var textInputLayer = GetWindowTextInputLayer();
			textInputLayer.Remove(_gtkEntry);
		}		
	}
}
