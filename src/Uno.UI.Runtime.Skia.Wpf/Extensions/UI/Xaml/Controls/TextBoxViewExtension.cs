
#nullable enable

using System;
using System.Windows;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.WPF.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Point = Windows.Foundation.Point;
using WpfCanvas = System.Windows.Controls.Canvas;
using Uno.UI.XamlHost.Skia.Wpf.Hosting;
using Uno.Disposables;

namespace Uno.UI.Runtime.Skia.WPF.Extensions.UI.Xaml.Controls
{
	internal class TextBoxViewExtension : ITextBoxViewExtension
	{
		private readonly TextBoxView _owner;
		private ContentControl? _contentElement;

		private WpfTextViewTextBox? _currentTextBoxInputWidget;
		private System.Windows.Controls.PasswordBox? _currentPasswordBoxInputWidget;

		private SerialDisposable _textChangedDisposable = new SerialDisposable();

		private readonly bool _isPasswordBox;
		private bool _isPasswordRevealed;

		public TextBoxViewExtension(TextBoxView owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
			_isPasswordBox = owner.TextBox is PasswordBox;
		}

		private WpfCanvas? GetWindowTextInputLayer()
		{
			if (_owner?.TextBox?.XamlRoot is not { } xamlRoot)
			{
				return null;
			}

			var host = XamlRootMap.GetHostForRoot(xamlRoot);
			return host?.NativeOverlayLayer;
		}

		public bool IsNativeOverlayLayerInitialized => GetWindowTextInputLayer() is not null;

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
				textInputLayer.Children.Add(_currentTextBoxInputWidget!);

				if (_isPasswordBox)
				{
					textInputLayer.Children.Add(_currentPasswordBoxInputWidget!);
					_currentPasswordBoxInputWidget!.Visibility = _isPasswordRevealed ? Visibility.Collapsed : Visibility.Visible;
					_currentTextBoxInputWidget!.Visibility = _isPasswordRevealed ? Visibility.Visible : Visibility.Collapsed;
				}
			}

			UpdateNativeView();
			SetTextNative(textBox.Text);
			ObserveInputChanges();
			InvalidateLayout();

			if (_isPasswordBox && !_isPasswordRevealed)
			{
				_currentPasswordBoxInputWidget!.Focus();
			}
			else
			{
				_currentTextBoxInputWidget!.Focus();
			}
		}

		public void EndEntry()
		{
			_textChangedDisposable.Disposable = null;
			if (_currentTextBoxInputWidget is null)
			{
				// No entry is in progress.
				return;
			}

			_owner.UpdateTextFromNative(_currentTextBoxInputWidget.Text);

			_contentElement = null;

			var textInputLayer = GetWindowTextInputLayer();
			if (textInputLayer is null)
			{
				return;
			}

			textInputLayer.Children.Remove(_currentTextBoxInputWidget);

			if (_currentPasswordBoxInputWidget is not null)
			{
				textInputLayer.Children.Remove(_currentPasswordBoxInputWidget);
			}
		}

		public void UpdateNativeView()
		{
			if ((_isPasswordBox && _currentPasswordBoxInputWidget == null) || _currentTextBoxInputWidget == null)
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

			updateCommon(_currentTextBoxInputWidget);
			updateCommon(_currentPasswordBoxInputWidget);
			SetForeground(textBox.Foreground);
			SetSelectionHighlightColor(textBox.SelectionHighlightColor);

			if (_currentTextBoxInputWidget is not null)
			{
				_currentTextBoxInputWidget.AcceptsReturn = textBox.AcceptsReturn;
				_currentTextBoxInputWidget.TextWrapping = textBox.AcceptsReturn ? TextWrapping.Wrap : TextWrapping.NoWrap;
				_currentTextBoxInputWidget.MaxLength = textBox.MaxLength;
				_currentTextBoxInputWidget.IsReadOnly = textBox.IsReadOnly;
			}

			if (_currentPasswordBoxInputWidget is not null)
			{
				_currentPasswordBoxInputWidget.MaxLength = textBox.MaxLength;
			}

			void updateCommon(System.Windows.Controls.Control? control)
			{
				if (control is null)
				{
					return;
				}

				control.FontSize = textBox.FontSize;
				control.FontWeight = FontWeight.FromOpenTypeWeight(textBox.FontWeight.Weight);
			}
		}

		public void InvalidateLayout()
		{
			UpdateSize();
			UpdatePosition();
		}

		public void UpdateSize()
		{
			var textInputLayer = GetWindowTextInputLayer();
			if (_contentElement == null || textInputLayer == null)
			{
				return;
			}

			updateSizeCore(_currentTextBoxInputWidget);
			updateSizeCore(_currentPasswordBoxInputWidget);

			void updateSizeCore(FrameworkElement? frameworkElement)
			{
				if (frameworkElement is not null && textInputLayer.Children.Contains(frameworkElement))
				{
					frameworkElement.Width = _contentElement.ActualWidth;
					frameworkElement.Height = _contentElement.ActualHeight;
				}
			}
		}

		public void UpdatePosition()
		{
			var textInputLayer = GetWindowTextInputLayer();
			if (_contentElement == null || textInputLayer == null)
			{
				return;
			}

			var transformToRoot = _contentElement.TransformToVisual(Microsoft.UI.Xaml.Window.Current.Content);
			var point = transformToRoot.TransformPoint(new Point(0, 0));

			updatePositionCore(_currentTextBoxInputWidget);
			updatePositionCore(_currentPasswordBoxInputWidget);

			void updatePositionCore(FrameworkElement? frameworkElement)
			{
				if (frameworkElement is not null && textInputLayer.Children.Contains(frameworkElement))
				{
					WpfCanvas.SetLeft(frameworkElement, point.X);
					WpfCanvas.SetTop(frameworkElement, point.Y);
				}
			}
		}

		public void SetTextNative(string text)
		{
			if (_currentTextBoxInputWidget != null && _currentTextBoxInputWidget.Text != text)
			{
				_currentTextBoxInputWidget.Text = text;
			}

			if (_currentPasswordBoxInputWidget != null && _currentPasswordBoxInputWidget.Password != text)
			{
				_currentPasswordBoxInputWidget.Password = text;
			}
		}

		private void EnsureWidgetForAcceptsReturn()
		{
			_currentTextBoxInputWidget ??= CreateInputControl();
			if (_isPasswordBox)
			{
				_currentPasswordBoxInputWidget ??= CreatePasswordControl();
				_currentTextBoxInputWidget.Visibility = Visibility.Collapsed;
			}
		}

		private WpfTextViewTextBox CreateInputControl()
		{
			var textView = new WpfTextViewTextBox();
			return textView;
		}

		private System.Windows.Controls.PasswordBox CreatePasswordControl()
		{
			var passwordBox = new System.Windows.Controls.PasswordBox();
			passwordBox.BorderBrush = System.Windows.Media.Brushes.Transparent;
			passwordBox.Background = System.Windows.Media.Brushes.Transparent;
			passwordBox.BorderThickness = new Thickness(0);
			return passwordBox;
		}

		private void ObserveInputChanges()
		{
			_textChangedDisposable.Disposable = null;
			CompositeDisposable disposable = new();
			if (_currentTextBoxInputWidget is not null)
			{
				_currentTextBoxInputWidget.TextChanged += WpfTextViewTextChanged;
				disposable.Add(Disposable.Create(() => _currentTextBoxInputWidget.TextChanged -= WpfTextViewTextChanged));
			}

			if (_currentPasswordBoxInputWidget is not null)
			{
				_currentPasswordBoxInputWidget.PasswordChanged += PasswordBoxViewPasswordChanged;
				disposable.Add(Disposable.Create(() => _currentPasswordBoxInputWidget.PasswordChanged -= PasswordBoxViewPasswordChanged));
			}
			_textChangedDisposable.Disposable = disposable;
		}

		private void PasswordBoxViewPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
		{
			_owner.UpdateTextFromNative(_currentPasswordBoxInputWidget!.Password);
		}

		private void WpfTextViewTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			_owner.UpdateTextFromNative(_currentTextBoxInputWidget!.Text);
		}

		public void SetIsPassword(bool isPassword)
		{
			_isPasswordRevealed = !isPassword;
			if (_owner.TextBox is { } textBox)
			{
				if (_currentTextBoxInputWidget is not null)
				{
					_currentTextBoxInputWidget.Visibility = isPassword ? Visibility.Collapsed : Visibility.Visible;
					if (_currentPasswordBoxInputWidget is not null)
					{
						_currentPasswordBoxInputWidget.Visibility = isPassword ? Visibility.Visible : Visibility.Collapsed;
					}

				}
			}
		}

		public void Select(int start, int length)
		{
			if (_isPasswordBox)
			{
				return;
			}

			if (_currentTextBoxInputWidget == null)
			{
				this.StartEntry();
			}

			_currentTextBoxInputWidget!.Select(start, length);
		}

		public int GetSelectionStart()
		{
			if (!_isPasswordBox)
			{
				return _currentTextBoxInputWidget?.SelectionStart ?? 0;
			}

			return 0;
		}

		public int GetSelectionLength()
		{
			if (!_isPasswordBox)
			{
				return _currentTextBoxInputWidget?.SelectionLength ?? 0;
			}

			return 0;
		}

		public void SetForeground(Microsoft.UI.Xaml.Media.Brush brush)
		{
			var wpfBrush = brush.ToWpfBrush();
			if (_currentTextBoxInputWidget != null)
			{
				_currentTextBoxInputWidget.Foreground = wpfBrush;
				_currentTextBoxInputWidget.CaretBrush = wpfBrush;
			}

			if (_currentPasswordBoxInputWidget != null)
			{
				_currentPasswordBoxInputWidget.Foreground = wpfBrush;
				_currentPasswordBoxInputWidget.CaretBrush = wpfBrush;
			}
		}

		public void SetSelectionHighlightColor(Microsoft.UI.Xaml.Media.Brush brush)
		{
			var wpfBrush = brush.ToWpfBrush();
			if (_currentTextBoxInputWidget != null)
			{
				_currentTextBoxInputWidget.SelectionBrush = wpfBrush;
			}

			if (_currentPasswordBoxInputWidget != null)
			{
				_currentPasswordBoxInputWidget.SelectionBrush = wpfBrush;
			}
		}
	}
}
