#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using Gtk;
using Pango;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.GTK.UI.Text;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using GdkPoint = Gdk.Point;
using GdkSize = Gdk.Size;
using GtkWindow = Gtk.Window;
using Point = Windows.Foundation.Point;
using Scale = Pango.Scale;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls
{
	internal class TextBoxViewExtension : ITextBoxViewExtension
	{
		private const string TextBoxViewCssClass = "textboxview";

		private static bool _warnedAboutSelectionColorChanges;

		private readonly string _textBoxViewId = Guid.NewGuid().ToString();
		private readonly TextBoxView _owner;
		private readonly GtkWindow _window;

		private CssProvider? _foregroundCssProvider;
		private ContentControl? _contentElement;
		private Widget? _currentInputWidget;
		private bool _handlingTextChanged;
		private GdkPoint _lastPosition = new GdkPoint(-1, -1);
		private GdkSize _lastSize = new GdkSize(-1, -1);

		private int? _selectionStartCache;
		private int? _selectionLengthCache;

		private readonly SerialDisposable _textChangedDisposable = new SerialDisposable();

		public TextBoxViewExtension(TextBoxView owner, GtkWindow window)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
			_window = window ?? throw new ArgumentNullException(nameof(window));
		}

		public bool IsNativeOverlayLayerInitialized => GetWindowTextInputLayer() is not null;

		public static TextBoxViewExtension? ActiveTextBoxView { get; private set; }

		private Fixed GetWindowTextInputLayer()
		{
			// now we have the GtkEventBox
			var overlay = (Overlay)((EventBox)_window.Child).Child;
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
			EnsureWidget(textBox);
			ObserveTextChanges();
			var textInputLayer = GetWindowTextInputLayer();
			if (_currentInputWidget!.Parent != textInputLayer)
			{
				textInputLayer.Put(_currentInputWidget!, 0, 0);
			}
			_lastSize = new GdkSize(-1, -1);
			_lastPosition = new GdkPoint(-1, -1);
			UpdateNativeView();
			SetWidgetText(textBox.Text);
			InvalidateLayout();

			textInputLayer.ShowAll();
			_currentInputWidget!.HasFocus = true;

			// Selection is now handled by native control
			if (_selectionStartCache != null && _selectionLengthCache != null)
			{
				Select(_selectionStartCache.Value, _selectionLengthCache.Value);
			}
			else
			{
				// Select end of the text
				var endIndex = textBox.Text.Length;
				Select(endIndex, 0);
			}
			_selectionStartCache = null;
			_selectionLengthCache = null;
		}

		public void EndEntry()
		{
			_textChangedDisposable.Disposable = null;
			if (GetInputText() is { } inputText)
			{
				_owner.UpdateTextFromNative(inputText);
			}

			_contentElement = null;

			if (_currentInputWidget != null)
			{
				var bounds = GetNativeSelectionBounds();
				(_selectionStartCache, _selectionLengthCache) = (bounds.start, bounds.end - bounds.start);
				var textInputLayer = GetWindowTextInputLayer();
				textInputLayer.Remove(_currentInputWidget);
				RemoveForegroundCssProvider();
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

			EnsureWidget(textBox);

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

		public void InvalidateLayout()
		{
			UpdateSize();
			UpdatePosition();
		}

		public void UpdateSize()
		{
			if (_contentElement == null || _currentInputWidget == null)
			{
				return;
			}

			var textInputLayer = GetWindowTextInputLayer();
			if (!textInputLayer.Children.Contains(_currentInputWidget))
			{
				return;
			}

			var width = (int)_contentElement.ActualWidth;
			var height = (int)_contentElement.ActualHeight;

			if (_lastSize.Width != width || _lastSize.Height != height)
			{
				_lastSize = new GdkSize(width, height);
				_currentInputWidget?.SetSizeRequest(_lastSize.Width, _lastSize.Height);
			}
		}

		public void UpdatePosition()
		{
			if (_contentElement == null || _currentInputWidget == null)
			{
				return;
			}

			var textInputLayer = GetWindowTextInputLayer();
			if (!textInputLayer.Children.Contains(_currentInputWidget))
			{
				return;
			}

			var transformToRoot = _contentElement.TransformToVisual(Microsoft.UI.Xaml.Window.Current.Content);
			var point = transformToRoot.TransformPoint(new Point(0, 0));
			var pointX = point.X;
			var pointY = point.Y;

			if (_lastPosition.X != pointX || _lastPosition.Y != pointY)
			{
				_lastPosition = new GdkPoint((int)pointX, (int)pointY);
				textInputLayer.Move(_currentInputWidget, _lastPosition.X, _lastPosition.Y);
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

		private void EnsureWidget(TextBox textBox)
		{
			var isPassword = false;
			var isPasswordVisible = true;
			if (textBox is PasswordBox passwordBox)
			{
				isPassword = true;
				isPasswordVisible = passwordBox.PasswordRevealMode == PasswordRevealMode.Visible;
			}

			// On UWP, A PasswordBox doesn't have AcceptsReturn property.
			// The property exists on Uno because PasswordBox incorrectly inherits TextBox.
			// If we have PasswordBox, ignore AcceptsReturnValue and always use Gtk.Entry
			var acceptsReturn = textBox.AcceptsReturn && !isPassword;

			var isIncompatibleInputType =
				(acceptsReturn && !(_currentInputWidget is TextView)) ||
				(!acceptsReturn && !(_currentInputWidget is Entry));
			if (isIncompatibleInputType)
			{
				var inputText = GetInputText();
				_currentInputWidget = CreateInputWidget(acceptsReturn, isPassword, isPasswordVisible);
				SetWidgetText(inputText ?? string.Empty);
			}
			SetForeground(textBox.Foreground);
			SetSelectionHighlightColor(textBox.SelectionHighlightColor);
		}

		private Widget CreateInputWidget(bool acceptsReturn, bool isPassword, bool isPasswordVisible)
		{
			Debug.Assert(!acceptsReturn || !isPassword);
			Widget widget;
			if (acceptsReturn)
			{
				var textView = new TextView();
				widget = textView;
			}
			else
			{
				var entry = new Entry();
				if (isPassword)
				{
					entry.InputPurpose = InputPurpose.Password;
					entry.Visibility = isPasswordVisible;
				}

				widget = entry;
			}
			widget.StyleContext.AddClass(TextBoxViewCssClass);
			return widget;
		}

		private void ObserveTextChanges()
		{
			if (_currentInputWidget is TextView textView)
			{
				textView.Buffer.Changed += WidgetTextChanged;
				_textChangedDisposable.Disposable = Disposable.Create(() =>
				{
					textView.Buffer.Changed -= WidgetTextChanged;
				});
			}
			else if (_currentInputWidget is Entry entry)
			{
				entry.Changed += WidgetTextChanged;
				_textChangedDisposable.Disposable = Disposable.Create(() =>
				{
					entry.Changed -= WidgetTextChanged;
				});
			}
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
					// Avoid setting same text (as it raises WidgetTextChanged on GTK).
					if (entry.Text != text)
					{
						entry.Text = text;
					}
					break;
				case TextView textView:
					// Avoid setting same text (as it raises WidgetTextChanged on GTK).
					if (textView.Buffer.Text != text)
					{
						textView.Buffer.Text = text;
					}
					break;
			};
		}

		public void SetIsPassword(bool isPassword)
		{
			if (_currentInputWidget is Entry entry)
			{
				entry.Visibility = !isPassword;
			}
		}

		public void Select(int start, int length)
		{
			var textBox = _owner.TextBox;
			if (textBox == null)
			{
				return;
			}

			EnsureWidget(textBox);
			if (textBox.FocusState == FocusState.Unfocused)
			{
				// Native control can't handle selection until it is part of visual tree.
				// Use managed selection until then.
				_selectionStartCache = textBox.Text.Length >= start ? start : textBox.Text.Length;
				_selectionLengthCache = textBox.Text.Length >= start + length ? length : textBox.Text.Length - start;
			}
			else
			{
				SetNativeSelectionBounds(start, start + length);
			}
		}

		private void SetNativeSelectionBounds(int start, int end)
		{
			if (_currentInputWidget is Entry entry)
			{
				entry.SelectRegion(start_pos: start, end_pos: end);
			}
			else if (_currentInputWidget is TextView textView)
			{
				var startIterator = textView.Buffer.GetIterAtOffset(start);
				var endIterator = textView.Buffer.GetIterAtOffset(end);
				textView.Buffer.SelectRange(startIterator, endIterator);
			}
		}

		private (int start, int end) GetNativeSelectionBounds()
		{
			if (_currentInputWidget is Entry entry)
			{
				entry.GetSelectionBounds(out var start, out var end);
				return (start, end);
			}
			else if (_currentInputWidget is TextView textView)
			{
				textView.Buffer.GetSelectionBounds(out var start, out var end);
				return (start.Offset, end.Offset); // TODO: Confirm this implementation is correct.
			}

			return (0, 0);
		}

		public int GetSelectionStart()
		{
			var textBox = _owner.TextBox;
			if (textBox == null)
			{
				return 0;
			}

			if (textBox.FocusState == FocusState.Unfocused)
			{
				return _selectionStartCache ?? 0;
			}
			else
			{
				return GetNativeSelectionBounds().start;
			}
		}

		public int GetSelectionLength()
		{
			var textBox = _owner.TextBox;
			if (textBox == null)
			{
				return 0;
			}

			if (textBox.FocusState == FocusState.Unfocused)
			{
				return _selectionLengthCache ?? 0;
			}
			else
			{
				var bounds = GetNativeSelectionBounds();
				return bounds.end - bounds.start;
			}
		}

		public void SetForeground(Brush brush)
		{
			if (_currentInputWidget is not null && brush is SolidColorBrush scb)
			{
				RemoveForegroundCssProvider();
				_foregroundCssProvider = new CssProvider();
				var color = $"rgba({scb.ColorWithOpacity.R},{scb.ColorWithOpacity.G},{scb.ColorWithOpacity.B},{scb.ColorWithOpacity.A})";
				var cssClassName = $"textbox_foreground_{_textBoxViewId}";
				var data = $".{cssClassName}, .{cssClassName} text {{ caret-color: {color}; color: {color}; }}";
				_foregroundCssProvider.LoadFromData(data);
				StyleContext.AddProviderForScreen(Gdk.Screen.Default, _foregroundCssProvider, priority: uint.MaxValue);
				if (!_currentInputWidget.StyleContext.HasClass(cssClassName))
				{
					_currentInputWidget.StyleContext.AddClass(cssClassName);
				}
			}
		}

		private void RemoveForegroundCssProvider()
		{
			if (_foregroundCssProvider is not null)
			{
				StyleContext.RemoveProviderForScreen(Gdk.Screen.Default, _foregroundCssProvider);
				_foregroundCssProvider.Dispose();
				_foregroundCssProvider = null;
			}
		}

		public void SetSelectionHighlightColor(Brush brush)
		{
			if (!_warnedAboutSelectionColorChanges)
			{
				_warnedAboutSelectionColorChanges = true;
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					// Selection highlight color change is not supported on GTK currently
					this.Log().LogWarning("SelectionHighlightColor changes are currently not supported on GTK");
				}
			}
		}
	}
}
