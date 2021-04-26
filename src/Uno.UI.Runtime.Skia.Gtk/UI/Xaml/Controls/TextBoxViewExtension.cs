#nullable enable

using System;
using System.Linq;
using Gtk;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Xaml.Controls;
using GLib;
using Pango;
using Uno.Disposables;
using Uno.UI.Runtime.Skia.GTK.UI.Text;
using GtkWindow = Gtk.Window;
using Object = GLib.Object;
using Point = Windows.Foundation.Point;
using Scale = Pango.Scale;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls
{
	internal class TextBoxViewExtension : ITextBoxViewExtension
	{
		private const string TextBoxViewCssClass = "textboxview";

		private readonly TextBoxView _owner;
		private readonly GtkWindow _window;
		private ContentControl? _contentElement;
		private Widget? _currentInputWidget;
		private bool _handlingTextChanged;

		private readonly SerialDisposable _textChangedDisposable = new SerialDisposable();
		private readonly SerialDisposable _textBoxEventSubscriptions = new SerialDisposable();

		public TextBoxViewExtension(TextBoxView owner, GtkWindow window)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
			_window = window ?? throw new ArgumentNullException(nameof(window));
		}

		private Fixed GetWindowTextInputLayer()
		{
			// now we have the GtkEventBox
			var overlay = (Overlay)((EventBox) _window.Child).Child;
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

			textBox.SizeChanged += ContentElementSizeChanged;
			textBox.LayoutUpdated += ContentElementLayoutUpdated;
			_textBoxEventSubscriptions.Disposable = Disposable.Create(() =>
			{
				textBox.SizeChanged -= ContentElementSizeChanged;
				textBox.LayoutUpdated -= ContentElementLayoutUpdated;
			});

			UpdateNativeView();
			SetWidgetText(textBox.Text);

			UpdateSize();
			UpdatePosition();

			textInputLayer.ShowAll();
			_currentInputWidget!.HasFocus = true;
		}

		public void EndEntry()
		{
			if (GetInputText() is { } inputText)
			{
				_owner.UpdateTextFromNative(inputText);
			}

			_contentElement = null;
			_textBoxEventSubscriptions.Disposable = null;

			if (_currentInputWidget != null)
			{
				var textInputLayer = GetWindowTextInputLayer();
				textInputLayer.Remove(_currentInputWidget);
			}
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

			var fontDescription = new FontDescription
			{
				Weight = textBox.FontWeight.ToPangoWeight(),
				AbsoluteSize = textBox.FontSize * Scale.PangoScale,
			};
#pragma warning disable CS0612 // Type or member is obsolete
			_currentInputWidget.OverrideFont(fontDescription);
#pragma warning restore CS0612 // Type or member is obsolete

			switch (_currentInputWidget)
			{
				case Entry entry:
					UpdateEntryProperties(entry, textBox);
					break;
				case TextView textView:
					UpdateTextViewProperties(textView, textBox);
					break;
			}
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

		public void SetTextNative(string text) => SetWidgetText(text);

		private void UpdateTextViewProperties(TextView textView, TextBox textBox)
		{
			textView.Editable = !textBox.IsReadOnly;
		}

		private void UpdateEntryProperties(Entry entry, TextBox textBox)
		{
			entry.IsEditable = !textBox.IsReadOnly;
			entry.MaxLength = textBox.MaxLength;

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
				SetWidgetText(inputText ?? string.Empty);
			}
		}

		private Widget CreateInputWidget(bool acceptsReturn)
		{
			Widget widget;
			if (acceptsReturn)
			{
				var textView = new TextView();
				textView.Buffer.Changed += WidgetTextChanged;
				_textChangedDisposable.Disposable = Disposable.Create(() => textView.Buffer.Changed -= WidgetTextChanged);
				widget = textView;
			}
			else
			{
				var entry = new Entry();
				entry.Changed += WidgetTextChanged;
				_textChangedDisposable.Disposable = Disposable.Create(() => entry.Changed -= WidgetTextChanged);
				widget = entry;
			}
			widget.StyleContext.AddClass(TextBoxViewCssClass);
			return widget;
		}

		private void WidgetTextChanged(object? sender, EventArgs e)
		{
			// Avoid stack overflow as updating text from
			// shared code briefly sets empty string and causes
			// infinite loop
			if (_handlingTextChanged)
			{
				return;
			}

			try
			{
				_handlingTextChanged = true;
				_owner.UpdateTextFromNative(GetInputText() ?? string.Empty);

			}
			finally
			{
				_handlingTextChanged = false;
			}
		}

		private string? GetInputText() =>
			_currentInputWidget switch
			{
				Entry entry => entry.Text,
				TextView textView => textView.Buffer.Text,
				_ => null
			};

		private void SetWidgetText(string text)
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

		private void ContentElementLayoutUpdated(object? sender, object e)
		{
			UpdateSize();
			UpdatePosition();
		}

		private void ContentElementSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs args)
		{
			UpdateSize();
			UpdatePosition();
		}
	}
}
