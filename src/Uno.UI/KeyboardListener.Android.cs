using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Android.Views;
using System;
using Uno.UI.Extensions;

namespace Uno.UI
{
	internal class KeyboardListener
	{
		private UIElement _rootContent;
		private UIElement _focusedElement;

		public void SetRootContent(UIElement rootContent)
		{
			if (_rootContent == rootContent)
			{
				return; // nothing to do
			}

			if (_rootContent != null)
			{
				_rootContent.GotFocus -= OnFocus;
				_rootContent.LostFocus -= LostFocus;
				_rootContent.KeyPress -= OnKeyPressed;
			}

			if (rootContent != null)
			{
				rootContent.GotFocus += OnFocus;
				rootContent.LostFocus += LostFocus;
				rootContent.KeyPress += OnKeyPressed;
			}

			_rootContent = rootContent;
		}

		private void LostFocus(object sender, RoutedEventArgs e)
		{
			if (e.OriginalSource == _focusedElement)
			{
				OnFocus(null);
			}
		}

		private void OnFocus(object sender, RoutedEventArgs e)
		{
			OnFocus(e.OriginalSource as UIElement);
		}

		private void OnFocus(UIElement focusedView)
		{
			if (_focusedElement == focusedView)
			{
				return; // nothing to do
			}

			if (_focusedElement != null)
			{
				//_focusedElement.KeyPress -= OnKeyPressed;
			}

			if (focusedView != null)
			{
				//focusedView.KeyPress += OnKeyPressed;
			}

			_focusedElement = focusedView;
		}

		private void OnKeyPressed(object sender, View.KeyEventArgs e)
		{
			var element = _focusedElement;
			if (element == null)
			{
				return;
			}

			e.Handled = false;
			if (e.Event == null)
			{
				return;
			}

			var keyRoutedEventArgs = new KeyRoutedEventArgs()
			{
				Key = e.Event.KeyCode.ToVirtualKey()
			};

			if (e.Event.Action == KeyEventActions.Up)
			{
				element.RaiseEvent(UIElement.KeyUpEvent, keyRoutedEventArgs);
			}
			else if (e.Event.Action == KeyEventActions.Down)
			{
				element.RaiseEvent(UIElement.KeyDownEvent, keyRoutedEventArgs);
			}

			e.Handled = keyRoutedEventArgs.Handled;
		}
	}
}
