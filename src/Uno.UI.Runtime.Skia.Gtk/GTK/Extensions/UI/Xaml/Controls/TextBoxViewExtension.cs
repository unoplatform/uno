#nullable enable

using System;
using System.Linq;
using Gtk;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using GLib;
using GtkWindow = Gtk.Window;
using Object = GLib.Object;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls
{
	internal class TextBoxViewExtension : ITextBoxViewExtension
	{
		private const string TextBoxViewCssClass = "textboxview";

		private readonly TextBoxView _owner;
		private readonly GtkWindow _window;
		private ContentControl? _contentElement;
		private Widget? _currentInputWidget;

		public TextBoxViewExtension(TextBoxView owner, GtkWindow window)
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
			var textBox = _owner.TextBox;
			if (textBox == null)
			{
				// The parent TextBox must exist as source of properties.
				return;
			}

			_contentElement = textBox.ContentElement;

			EnsureWidgetForAcceptsReturn(textBox.AcceptsReturn);
			var textInputLayer = GetWindowTextInputLayer();
			textInputLayer.Put(_currentInputWidget!, 0, 0);

			_contentElement.SizeChanged += _contentElement_SizeChanged;
			_contentElement.LayoutUpdated += _contentElement_LayoutUpdated;
			UpdateNativeView();
			SetInputText(textBox.Text);

			UpdateSize();
			UpdatePosition();

			textInputLayer.ShowAll();
			_currentInputWidget!.HasFocus = true;
		}

		private void _contentElement_LayoutUpdated(object sender, object e)
		{
			UpdateSize();
			UpdatePosition();
		}

		private void _contentElement_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs args)
		{
			UpdateSize();
			UpdatePosition();
		}

		public void EndEntry()
		{
			if (GetInputText() is { } inputText)
			{
				_owner.UpdateText(inputText);
			}

			if (_contentElement != null)
			{
				_contentElement.SizeChanged -= _contentElement_SizeChanged;
				_contentElement.LayoutUpdated -= _contentElement_LayoutUpdated;
			}

			var textInputLayer = GetWindowTextInputLayer();
			textInputLayer.Remove(_currentInputWidget);
		}

		public void UpdateNativeView()
		{
			if (_currentInputWidget == null)
			{
				// If the input widget does not exist, we don't need to update it.
				return;
			}

			var textBox = _owner.TextBox;
			if (textBox == null)
			{
				// The parent TextBox must exist as source of properties.
				return;
			}

			EnsureWidgetForAcceptsReturn(textBox.AcceptsReturn);

			var fontSize = new Value(GType.Double) { Val = textBox.FontSize };
			_currentInputWidget.StyleContext.SetProperty("font-size", fontSize);
		}

		private void EnsureWidgetForAcceptsReturn(bool acceptsReturn)
		{
			var isIncompatibleInputType =
				(acceptsReturn && !(_currentInputWidget is TextView)) ||
				(!acceptsReturn && !(_currentInputWidget is Entry));
			if (isIncompatibleInputType)
			{
				var inputText = GetInputText();
				_currentInputWidget = CreateInputWidget(acceptsReturn);
				SetInputText(inputText ?? string.Empty);
			}
		}

		private Widget CreateInputWidget(bool acceptsReturn)
		{
			Widget widget;
			if (acceptsReturn)
			{
				widget = new TextView();
			}
			else
			{
				widget = new Entry();
			}
			widget.StyleContext.AddClass(TextBoxViewCssClass);
			return widget;
		}

		private string? GetInputText() =>
			_currentInputWidget switch
			{
				Entry entry => entry.Text,
				TextView textView => textView.Buffer.Text,
				_ => null
			};

		private void SetInputText(string text)
		{
			switch (_currentInputWidget)
			{
				case Entry entry:
					entry.Text = text;
					break;
				case TextView textView:
					textView.Buffer.Text = text;
					break;
			};
		}

		public void UpdateSize()
		{
			if (_contentElement == null || _currentInputWidget == null)
			{
				return;
			}

			var textInputLayer = GetWindowTextInputLayer();
			if (textInputLayer.Children.Contains(_currentInputWidget))
			{
				_currentInputWidget?.SetSizeRequest((int)_contentElement.ActualWidth, (int)_contentElement.ActualHeight);
			}
		}

		public void UpdatePosition()
		{
			if (_contentElement == null || _currentInputWidget == null)
			{
				return;
			}

			var transformToRoot = _contentElement.TransformToVisual(Windows.UI.Xaml.Window.Current.Content);
			var point = transformToRoot.TransformPoint(new Point(0, 0));
			var textInputLayer = GetWindowTextInputLayer();
			if (textInputLayer.Children.Contains(_currentInputWidget))
			{
				textInputLayer.Move(_currentInputWidget, (int)point.X, (int)point.Y);
			}
		}
	}
}
