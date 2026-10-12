using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using System;
using Microsoft.UI.Xaml;

namespace UITests.Shared.Windows_UI_Xaml_Input.RoutedEvents
{
	[Sample("Routed Events", Name = "Pointer Events")]
	public sealed partial class RoutedEvent_Pointer : Page
	{
		public RoutedEvent_Pointer()
		{
			this.InitializeComponent();

			AddHandler(PointerEnteredEvent, new PointerEventHandler(PointerEnteredHandler), handledEventsToo: true);
			AddHandler(PointerExitedEvent, new PointerEventHandler(PointerExitedHandler), handledEventsToo: true);
			AddHandler(PointerMovedEvent, new PointerEventHandler(PointerMovedHandler), handledEventsToo: true);
			AddHandler(PointerPressedEvent, new PointerEventHandler(PointerPressedHandler), handledEventsToo: true);
			AddHandler(PointerReleasedEvent, new PointerEventHandler(PointerReleasedHandler), handledEventsToo: true);
		}

		private void PointerEnteredHandler(object sender, PointerRoutedEventArgs e)
		{
			txtRoot.Text += $"ENTERED (root) - handledEventsToo: true, sender={GetName(sender)}, originalSource={e.OriginalSource}\n";
			_lastEventIsMoved = false;
		}

		private void PointerExitedHandler(object sender, PointerRoutedEventArgs e)
		{
			txtRoot.Text += $"EXITED (root) - handledEventsToo: true, sender={GetName(sender)}, originalSource={e.OriginalSource}\n";
			_lastEventIsMoved = false;
		}

		private bool _lastEventIsMoved = false;
		private void PointerMovedHandler(object sender, PointerRoutedEventArgs e)
		{
			if (_lastEventIsMoved)
			{
				return;
			}
			txtRoot.Text += $"MOVED (root) - handledEventsToo: true, sender={GetName(sender)}, originalSource={e.OriginalSource}\n";
			_lastEventIsMoved = true;
		}

		private void PointerPressedHandler(object sender, PointerRoutedEventArgs e)
		{
			txtRoot.Text += $"PRESSED (root) - handledEventsToo: true, sender={GetName(sender)}, originalSource={e.OriginalSource}\n";
			_lastEventIsMoved = false;
		}

		private void PointerReleasedHandler(object sender, PointerRoutedEventArgs e)
		{
			txtRoot.Text += $"RELEASED (root) - handledEventsToo: true, sender={GetName(sender)}, originalSource={e.OriginalSource}\n";
			_lastEventIsMoved = false;
		}

		private static string GetName(object element)
		{
			if (element == null)
			{
				return "<null>";
			}
			if (element is FrameworkElement fe)
			{
				return string.IsNullOrWhiteSpace(fe.Name) ? fe.ToString() : fe.Name;
			}

			return element.ToString();
		}

		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
			base.OnPointerEntered(args);

			txtRoot.Text += "ENTERED (root) - override, should not happen when tapped on children element\n";
		}

		protected override void OnPointerExited(PointerRoutedEventArgs args)
		{
			base.OnPointerExited(args);

			txtRoot.Text += "EXITED (root) - override, should not happen when tapped on children element\n";
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
			base.OnPointerMoved(args);

			if (_lastEventIsMoved)
			{
				return;
			}
			txtRoot.Text += "MOVED (root) - override, should not happen when tapped on children element\n";
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			txtRoot.Text += "PRESSED (root) - override, should not happen when tapped on children element\n";
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);

			txtRoot.Text += "RELEASED (root) - override, should not happen when tapped on children element\n";
		}
	}
}
