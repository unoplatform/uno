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
using GdkPoint = Gdk.Point;
using GdkSize = Gdk.Size;
using GtkWindow = Gtk.Window;
using Point = Windows.Foundation.Point;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls
{
	internal class TextBoxViewExtension : ITextBoxViewExtension
	{
		private readonly TextBoxView _owner;
		private readonly GtkWindow _window;
		
		private ContentControl? _contentElement;
		private ITextBoxView? _textBoxView;
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
			EnsureTextBoxView(textBox);
			ObserveNativeTextChanges();
			var textInputLayer = GetWindowTextInputLayer();
			_textBoxView!.AddToTextInputLayer(textInputLayer);
			_lastSize = new GdkSize(-1, -1);
			_lastPosition = new GdkPoint(-1, -1);
			UpdateNativeView();
			SetNativeText(textBox.Text);
			InvalidateLayout();

			textInputLayer.ShowAll();
			_textBoxView!.SetFocus(true);

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
			if (GetNativeText() is { } inputText)
			{
				_owner.UpdateTextFromNative(inputText);
			}

			_contentElement = null;
			
			if (_textBoxView != null)
			{
				var bounds = GetNativeSelectionBounds();
				(_selectionStartCache, _selectionLengthCache) = (bounds.start, bounds.end - bounds.start);
				_textBoxView.RemoveFromTextInputLayer();				
			}
		}

		public void SetText(string text) => SetNativeText(text);

		public void UpdateNativeView()
		{
			if (_textBoxView is null || _owner.TextBox is not { } textBox)
			{
				// If the input widget does not exist, we don't need to update it.
				// The parent TextBox must exist as source of properties.
				return;
			}

			EnsureTextBoxView(textBox);
		}

		public void InvalidateLayout()
		{
			UpdateSize();
			UpdatePosition();
		}

		public void UpdateProperties()
		{
			if (_owner?.TextBox is { } textBox)
			{
				_textBoxView?.UpdateProperties(textBox);
			}
		}
			
		public void UpdateSize()
		{
			if (_contentElement is null ||
				_textBoxView is null ||
				!_textBoxView.IsDisplayed)
			{
				return;
			}

			var width = (int)(_contentElement.ActualWidth - _contentElement.Padding.Horizontal());
			var height = (int)(_contentElement.ActualHeight - _contentElement.Padding.Vertical());

			if (_lastSize.Width != width || _lastSize.Height != height)
			{
				_lastSize = new GdkSize(width, height);
				_textBoxView.SetSize(_lastSize.Width, _lastSize.Height);
			}
		}

		public void UpdatePosition()
		{
			if (_contentElement == null ||
				_textBoxView == null ||
				!_textBoxView.IsDisplayed)
			{
				return;
			}
			
			var transformToRoot = _contentElement.TransformToVisual(Windows.UI.Xaml.Window.Current.Content);
			var point = transformToRoot.TransformPoint(new Point(_contentElement.Padding.Left, _contentElement.Padding.Top));
			var pointX = (int)point.X;
			var pointY = (int)point.Y;

			if (_lastPosition.X != pointX || _lastPosition.Y != pointY)
			{
				_lastPosition = new GdkPoint(pointX, pointY);
				_textBoxView.SetPosition(pointX, pointY);
			}
		}

		public void SetIsPassword(bool isPassword)
		{
			if (_textBoxView is Entry entry)
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

			EnsureTextBoxView(textBox);
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
	
		public int GetSelectionStart()
		{
			var textBox = _owner.TextBox;
			if (textBox == null)
			{
				return 0;
			}

			return textBox.FocusState == FocusState.Unfocused ?
				_selectionStartCache ?? 0 :
				GetNativeSelectionBounds().start;
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
		
		private void SetNativeSelectionBounds(int start, int end) => _textBoxView?.SetSelectionBounds(start, end);

		private (int start, int end) GetNativeSelectionBounds() => _textBoxView?.GetSelectionBounds() ?? (0, 0);
		
		private void EnsureTextBoxView(TextBox textBox)
		{
			if (_textBoxView is null ||
				!_textBoxView.IsCompatible(textBox))
			{
				var inputText = GetNativeText();
				_textBoxView = GtkTextBoxView.Create(textBox);
				SetNativeText(inputText ?? string.Empty);
			}

			_textBoxView.UpdateProperties(textBox);
		}

		private void ObserveNativeTextChanges()
		{
			_textChangedDisposable.Disposable = null;
			if (_textBoxView is not null)
			{
				_textChangedDisposable.Disposable = _textBoxView.ObserveTextChanges(NativeTextChanged);
			}
		}

		private void NativeTextChanged(object? sender, EventArgs e)
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
				_owner.UpdateTextFromNative(GetNativeText() ?? string.Empty);

			}
			finally
			{
				_handlingTextChanged = false;
			}
		}

		private string? GetNativeText() => _textBoxView?.Text;

		private void SetNativeText(string text)
		{
			if (_textBoxView is null)
			{
				return;
			}

			// Avoid setting same text (as it raises WidgetTextChanged on GTK).
			if (_textBoxView.Text != text)
			{
				_textBoxView.Text = text;
			}
		}
	}
}
