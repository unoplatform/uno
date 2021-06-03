using System;
using Windows.System;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class RepeatButton : ButtonBase
	{
		private bool _keyboardCausingRepeat;
		private bool _pointerCausingRepeat;
		private DispatcherTimer _timer;

		public int Interval
		{
			get => (int)GetValue(IntervalProperty);
			set => SetValue(IntervalProperty, value);
		}

		public static DependencyProperty IntervalProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(Interval),
			propertyType: typeof(int),
			ownerType: typeof(RepeatButton),
			typeMetadata: new FrameworkPropertyMetadata(250, propertyChangedCallback: (s, e) => (s as RepeatButton)?.OnIntervalChanged(e)));

		public int Delay
		{
			get => (int)this.GetValue(DelayProperty);
			set => this.SetValue(DelayProperty, value);
		}

		public static DependencyProperty DelayProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(Delay),
			propertyType: typeof(int),
			ownerType: typeof(RepeatButton),
			typeMetadata: new FrameworkPropertyMetadata(250, propertyChangedCallback: (s, e) => (s as RepeatButton)?.OnDelayChanged(e)));

		internal bool IgnoreTouchInput { get; set; }

		public RepeatButton() : base()
		{
			InitializeVisualStates();

			ClickMode = ClickMode.Press;

			DefaultStyleKey = typeof(RepeatButton);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
			=> new RepeatButtonAutomationPeer(this);

		private void OnIntervalChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is int newValue)
			{
				if (newValue <= 0)
				{
					throw new ArgumentException($"Interval cannot be positive", DelayProperty.ToString());
				}
			}
		}

		private void OnDelayChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is int newValue)
			{
				if (newValue < 0)
				{
					throw new ArgumentException($"Delay cannot be negative", DelayProperty.ToString());
				}
			}
		}

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);

			_keyboardCausingRepeat = false;
			_pointerCausingRepeat = false;

			UpdateRepeatState();
		}

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			if(args.Key == VirtualKey.Space
				&& ClickMode != ClickMode.Hover)
			{
				_keyboardCausingRepeat = true;
				UpdateRepeatState();
			}
		}

		protected override void OnKeyUp(KeyRoutedEventArgs args)
		{
			base.OnKeyUp(args);

			if (args.Key == VirtualKey.Space
				& ClickMode != ClickMode.Hover)
			{
				_keyboardCausingRepeat = false;
				UpdateRepeatState();
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);

			if (ClickMode != ClickMode.Hover)
			{
				_keyboardCausingRepeat = false;
				_pointerCausingRepeat = false;
				UpdateRepeatState();
			}
		}

		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
			base.OnPointerEntered(args);

			if (ClickMode == ClickMode.Hover)
			{
				_pointerCausingRepeat = true;
			}

			UpdateRepeatState();
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
			base.OnPointerMoved(args);
		}

		protected override void OnPointerExited(PointerRoutedEventArgs args)
		{
			base.OnPointerExited(args);

			if (ClickMode == ClickMode.Hover)
			{
				_pointerCausingRepeat = false;
				UpdateRepeatState();
			}
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			if (args.Handled)
			{
				return;
			}

			base.OnPointerPressed(args);

			var pointerPoint = args.GetCurrentPoint(this);
			var isLeftButtonPressed = pointerPoint.Properties.IsLeftButtonPressed;

			if (isLeftButtonPressed)
			{
				if (ClickMode != ClickMode.Hover)
				{
					_pointerCausingRepeat = true;
					UpdateRepeatState();
				}
			}
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			if (args.Handled)
			{
				return;
			}

			base.OnPointerReleased(args);

			if (ClickMode != ClickMode.Hover)
			{
				_pointerCausingRepeat = false;
				UpdateRepeatState();
			}
		}

		private void StartTimer()
		{

			if (_timer == null)
			{
				_timer = new DispatcherTimer();
				_timer.Tick += TimerCallback;
			}

			if (!_timer.IsEnabled)
			{
				_timer.Interval = TimeSpan.FromMilliseconds(Delay);
				_timer.Start();
			}
		}

		private void StopTimer()
		{
			_timer?.Stop();
		}

		private void UpdateRepeatState()
		{
			if((_pointerCausingRepeat && IsPointerOver) || _keyboardCausingRepeat)
			{
				StartTimer();
			}
			else
			{
				StopTimer();
			}
		}

		private void TimerCallback(object sender, object state)
		{
			var interval = TimeSpan.FromMilliseconds(Interval);

			if (_timer.Interval != interval)
			{
				_timer.Interval = interval;
			}

			var isPressed = IsPressed;

			if ((isPressed && IsPointerOver) || (isPressed && _keyboardCausingRepeat))
			{
				RaiseClick();
			}
			else
			{
				StopTimer();
			}
		}
	}
}
