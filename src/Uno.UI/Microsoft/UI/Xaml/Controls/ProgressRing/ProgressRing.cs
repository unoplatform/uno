using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Logging;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ProgressRing : Control
	{
		private readonly ILottieVisualSourceProvider _lottieProvider;

		public static DependencyProperty IsActiveProperty { get ; } = DependencyProperty.Register(
			"IsActive", typeof(bool), typeof(ProgressRing), new FrameworkPropertyMetadata(true, OnIsActivePropertyChanged));

		private AnimatedVisualPlayer _player;
		private Panel _layoutRoot;

		public bool IsActive
		{
			get => (bool)GetValue(IsActiveProperty);
			set => SetValue(IsActiveProperty, value);
		}

		public ProgressRing()
		{
			DefaultStyleKey = typeof(ProgressRing);

			ApiExtensibility.CreateInstance(this, out _lottieProvider);

			if (_lottieProvider == null)
			{
				this.Log().Error($"{nameof(ProgressRing)} control needs the Uno.UI.Lottie package to run properly.");
			}

			RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundPropertyChanged);
			RegisterPropertyChangedCallback(BackgroundProperty, OnbackgroundPropertyChanged);
		}

		protected override AutomationPeer OnCreateAutomationPeer() => new ProgressRingAutomationPeer(progressRing: this);

		protected override void OnApplyTemplate()
		{
			_player = GetTemplateChild("IndeterminateAnimatedVisualPlayer") as Windows.UI.Xaml.Controls.AnimatedVisualPlayer;
			_layoutRoot = GetTemplateChild("LayoutRoot") as Panel;

			SetAnimatedVisualPlayerSource();

			ChangeVisualState();
		}

		private void OnForegroundPropertyChanged(DependencyObject sender, DependencyProperty dp)
		{
			if (Background is SolidColorBrush background)
			{
				// TODO
			}
		}

		private void OnbackgroundPropertyChanged(DependencyObject sender, DependencyProperty dp)
		{
			if (Foreground is SolidColorBrush foreground)
			{
				// TODO
			}
		}

		private static void OnIsActivePropertyChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			(dependencyobject as ProgressRing)?.ChangeVisualState();
		}

		private void SetAnimatedVisualPlayerSource()
		{
			if (_lottieProvider != null && _player != null)
			{
				var animatedVisualSource = _lottieProvider.CreateFromLottieAsset(FeatureConfiguration.ProgressRing.ProgressRingAsset);
				_player.Source = animatedVisualSource;
				ChangeVisualState();
			}
			else if (_player != null && _layoutRoot != null)
			{
				// If we have a _player, it means we're having a ControlTemplate relying
				// on it.  In this case, the Uno.UI.Lottie reference is required for the
				// rendering of the ProgressRing.

				var txt = new TextBlock
				{
					Text = "⚠️ Uno.UI.Lottie missing ⚠️",
					Foreground = SolidColorBrushHelper.Red
				};

				_layoutRoot.Children.Add(txt);
			}
		}

		private void ChangeVisualState()
		{
			if (IsActive)
			{
				// Support for older templates
				VisualStateManager.GoToState(this, "Active", true);

				var _ = _player?.PlayAsync(0, 1, true);
			}
			else
			{
				VisualStateManager.GoToState(this, "Inactive", true);
				_player?.Stop();
			}
		}
	}
}
