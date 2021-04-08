#nullable enable

using System;
using Windows.UI.Xaml.Controls;
using Uno.UI.Skia.Platform;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace Uno.UI.Runtime.Skia.WPF.Extensions.UI.Xaml.Controls
{
	internal class TextBoxViewExtension : ITextBoxViewExtension
	{
		private readonly TextBoxView _owner;
		private ContentControl? _contentElement;
		private WpfTextBox? _currentInputWidget;

		public TextBoxViewExtension(TextBoxView owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}

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
			textInputLayer.Children.Add(_currentInputWidget!);

			_contentElement.SizeChanged += _contentElement_SizeChanged;
			_contentElement.LayoutUpdated += _contentElement_LayoutUpdated;
			UpdateNativeView();
			SetInputText(textBox.Text);

			UpdateSize();
			UpdatePosition();

			_currentInputWidget!.Focus();
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
		}

		private void EnsureWidgetForAcceptsReturn()
		{
			_currentInputWidget ??= CreateInputControl();
		}

		private WpfTextBox CreateInputControl()
		{
			return new WpfTextBox();
		}

		private string? GetInputText() => _currentInputWidget?.Text;

		private void SetInputText(string text)
		{
			if (_currentInputWidget != null)
			{
				_currentInputWidget.Text = text;
			}
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
	}
}
