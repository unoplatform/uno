#nullable enable

using System;
using System.Windows;
using Windows.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.UI.Runtime.Skia.WPF.Controls;
using Uno.UI.Skia.Platform;
using Uno.UI.Xaml.Controls.Extensions;
using Point = Windows.Foundation.Point;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace Uno.UI.Runtime.Skia.WPF.Extensions.UI.Xaml.Controls
{
	internal class TextBoxViewExtension : ITextBoxViewExtension
	{
		private readonly TextBoxView _owner;
		private ContentControl? _contentElement;
		private WpfTextViewTextBox? _currentInputWidget;

		public TextBoxViewExtension(TextBoxView owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		public bool IsNativeOverlayLayerInitialized => GetWindowTextInputLayer() is not null;
		
		private WpfCanvas? GetWindowTextInputLayer() => WpfHost.Current?.NativeOverlayLayer;

		public void StartEntry()
		{
			var textInputLayer = GetWindowTextInputLayer();
			var textBox = _owner.TextBox;
			if (textBox == null || textInputLayer == null)
			{
				// The parent TextBox must exist as source of properties.
				return;
			}

			_contentElement = textBox.ContentElement;

			EnsureWidgetForAcceptsReturn();

			if (textInputLayer.Children.Count == 0)
			{
				textInputLayer.Children.Add(_currentInputWidget!);
			}

			UpdateNativeView();
			SetTextNative(textBox.Text);

			InvalidateLayout();

			_currentInputWidget!.Focus();
		}

		public void EndEntry()
		{
			if (GetInputText() is { } inputText)
			{
				_owner.UpdateTextFromNative(inputText);
			}

			_contentElement = null;

			var textInputLayer = GetWindowTextInputLayer();
			textInputLayer?.Children.Remove(_currentInputWidget);
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

			EnsureWidgetForAcceptsReturn();

			_currentInputWidget.FontSize = textBox.FontSize;
			_currentInputWidget.FontWeight = FontWeight.FromOpenTypeWeight(textBox.FontWeight.Weight);
			_currentInputWidget.AcceptsReturn = textBox.AcceptsReturn;
			_currentInputWidget.TextWrapping = textBox.AcceptsReturn ? TextWrapping.Wrap : TextWrapping.NoWrap;
			_currentInputWidget.MaxLength = textBox.MaxLength;
			_currentInputWidget.IsReadOnly = textBox.IsReadOnly;
			_currentInputWidget.Foreground = textBox.Foreground.ToWpfBrush();
		}

		public void InvalidateLayout()
		{
			UpdateSize();
			UpdatePosition();
		}

		public void UpdateSize()
		{
			var textInputLayer = GetWindowTextInputLayer();
			if (_contentElement == null || _currentInputWidget == null || textInputLayer == null)
			{
				return;
			}

			if (textInputLayer.Children.Contains(_currentInputWidget))
			{
				_currentInputWidget.Width = _contentElement.ActualWidth;
				_currentInputWidget.Height = _contentElement.ActualHeight;
			}
		}

		public void UpdatePosition()
		{
			var textInputLayer = GetWindowTextInputLayer();
			if (_contentElement == null || _currentInputWidget == null || textInputLayer == null)
			{
				return;
			}

			var transformToRoot = _contentElement.TransformToVisual(Windows.UI.Xaml.Window.Current.Content);
			var point = transformToRoot.TransformPoint(new Point(0, 0));
			if (textInputLayer.Children.Contains(_currentInputWidget))
			{
				WpfCanvas.SetLeft(_currentInputWidget, point.X);
				WpfCanvas.SetTop(_currentInputWidget, point.Y);
			}
		}

		public void SetTextNative(string text)
		{
			if (_currentInputWidget != null)
			{
				_currentInputWidget.Text = text;
			}
		}

		private void EnsureWidgetForAcceptsReturn()
		{
			_currentInputWidget ??= CreateInputControl();
		}

		private WpfTextViewTextBox CreateInputControl()
		{
			var textView = new WpfTextViewTextBox();
			textView.TextChanged += WpfTextViewTextChanged;
			return textView;
		}

		private void WpfTextViewTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			if (_currentInputWidget != null)
			{
				_owner.UpdateTextFromNative(_currentInputWidget.Text);
			}
		}

		private string? GetInputText() => _currentInputWidget?.Text;

		public void SetIsPassword(bool isPassword)
		{
			// No support for now.
		}

		public void Select(int start, int length)
		{
			if (_currentInputWidget == null)
			{
				this.StartEntry();
			}

			_currentInputWidget!.Select(start, length);
		}

		public int GetSelectionStart() => _currentInputWidget?.SelectionStart ?? 0;

		public int GetSelectionLength() => _currentInputWidget?.SelectionLength ?? 0;

		public void SetForeground(Windows.UI.Xaml.Media.Brush brush)
		{
			if (_currentInputWidget != null)
			{
				_currentInputWidget.Foreground = brush.ToWpfBrush();
			}
		}
	}
}
