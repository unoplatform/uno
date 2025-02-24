#nullable enable

using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class ProgressBar : RangeBase
	{
		private const string s_ErrorStateName = "Error";
		private const string s_PausedStateName = "Paused";
		private const string s_IndeterminateStateName = "Indeterminate";
		private const string s_IndeterminateErrorStateName = "IndeterminateError";
		private const string s_IndeterminatePausedStateName = "IndeterminatePaused";
		private const string s_DeterminateStateName = "Determinate";
		private const string s_UpdatingStateName = "Updating";

		public static DependencyProperty IsIndeterminateProperty { get; } = DependencyProperty.Register(
			nameof(IsIndeterminate), typeof(bool), typeof(ProgressBar), new FrameworkPropertyMetadata(false, OnIsIndeterminateChanged));

		public bool IsIndeterminate
		{
			get => (bool)GetValue(IsIndeterminateProperty);
			set => SetValue(IsIndeterminateProperty, value);
		}

		public static DependencyProperty ShowErrorProperty { get; } = DependencyProperty.Register(
			nameof(ShowError), typeof(bool), typeof(ProgressBar), new FrameworkPropertyMetadata(false, OnShowErrorChanged));

		public bool ShowError
		{
			get => (bool)GetValue(ShowErrorProperty);
			set => SetValue(ShowErrorProperty, value);
		}

		public static DependencyProperty ShowPausedProperty { get; } = DependencyProperty.Register(
			nameof(ShowPaused), typeof(bool), typeof(ProgressBar), new FrameworkPropertyMetadata(false, OnShowPausedChanged));

		public bool ShowPaused
		{
			get => (bool)GetValue(ShowPausedProperty);
			set => SetValue(ShowPausedProperty, value);
		}

		public static DependencyProperty TemplateSettingsProperty { get; } = DependencyProperty.Register(
			nameof(TemplateSettings), typeof(ProgressBarTemplateSettings), typeof(ProgressBar), new FrameworkPropertyMetadata(default(ProgressBarTemplateSettings)));

		public ProgressBarTemplateSettings TemplateSettings
		{
			get => (ProgressBarTemplateSettings)GetValue(TemplateSettingsProperty);
			set => SetValue(TemplateSettingsProperty, value);
		}

		private Grid? m_layoutRoot;
		private Rectangle? m_determinateProgressBarIndicator;
		private Rectangle? m_indeterminateProgressBarIndicator;
		private Rectangle? m_indeterminateProgressBarIndicator2;
		private Size? m_previousMeasuredWidths;

		public ProgressBar()
		{
			DefaultStyleKey = typeof(ProgressBar);

			SizeChanged += (snd, evt) => OnSizeChange();

#if !UNO_HAS_ENHANCED_LIFECYCLE
			// Uno-specific: TODO: Investigate why we need this. It's a quite very old workaround from 2020
			// https://github.com/unoplatform/uno/commit/641bbc9483f33c64d5eddc474f069c07b79039ba
			// So, maybe it's no longer needed.
			// For now, we're sure it's no longer needed with enhanced lifecycle (actually, it's problematic if it exists there)
			// Note: LayoutUpdated event isn't really tied to a specific element. It really means that some element in the visual tree had a layout update.
			// So, this event subscription is wrong because it will cause the ProgressBar to transition to Updating visual state then back
			// to a state based on its properties (e.g, Indeterminate) every time any element in the visual tree has a layout update.
			// So it will cause some bad flickers in ProgressBar, and will also get us into a cycle in case there is a listener
			// to CurrentStateChanged event where the listener does something that updates the layout.
			LayoutUpdated += (snd, evt) => OnSizeChange();
#endif

			RegisterPropertyChangedCallback(ValueProperty, OnRangeBasePropertyChanged);
			RegisterPropertyChangedCallback(MinimumProperty, OnRangeBasePropertyChanged);
			RegisterPropertyChangedCallback(MaximumProperty, OnRangeBasePropertyChanged);
			RegisterPropertyChangedCallback(PaddingProperty, OnRangeBasePropertyChanged);

			TemplateSettings = new ProgressBarTemplateSettings();
		}

		protected override AutomationPeer OnCreateAutomationPeer() => new ProgressBarAutomationPeer(this);

		protected override void OnApplyTemplate()
		{
			m_layoutRoot = GetTemplateChild("LayoutRoot") as Grid;

			// NOTE: Example of how named parts are loaded from the template. Important to remember that it's possible for
			// any of them not to be found, since devs can replace the template with their own.

			m_determinateProgressBarIndicator = GetTemplateChild("DeterminateProgressBarIndicator") as Rectangle;
			m_indeterminateProgressBarIndicator = GetTemplateChild("IndeterminateProgressBarIndicator") as Rectangle;
			m_indeterminateProgressBarIndicator2 = GetTemplateChild("IndeterminateProgressBarIndicator2") as Rectangle;

			UpdateStates();
		}

		private void OnSizeChange()
		{
#if __ANDROID__ // Uno workaround for #12312: SetProgressBarIndicatorWidth raises LayoutUpdated, and they many loops to stabilize
			if (m_layoutRoot is not null &&
				m_determinateProgressBarIndicator is not null &&
				m_previousMeasuredWidths != new Size(m_layoutRoot.ActualWidth, m_determinateProgressBarIndicator.ActualWidth))
#endif
			{
				SetProgressBarIndicatorWidth();
			}

			UpdateWidthBasedTemplateSettings();
		}

		private void OnRangeBasePropertyChanged(DependencyObject sender, DependencyProperty dp)
		{
			// NOTE: This hits when the Value property changes, because we called RegisterPropertyChangedCallback.
			SetProgressBarIndicatorWidth();
		}

		private static void OnIsIndeterminateChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			// NOTE: This hits when IsIndeterminate changes because we set MUX_PROPERTY_CHANGED_CALLBACK to true in the idl.
			if (dependencyobject is ProgressBar progressBar)
			{
				progressBar.SetProgressBarIndicatorWidth();
				progressBar.UpdateStates();
			}
		}

		private static void OnShowErrorChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyobject is ProgressBar progressBar)
			{
				progressBar.UpdateStates();
			}
		}

		private static void OnShowPausedChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyobject is ProgressBar progressBar)
			{
				progressBar.UpdateStates();
			}
		}

		private void UpdateStates()
		{
			var showError = ShowError;
			var isIndeterminate = IsIndeterminate;

			if (showError && isIndeterminate)
			{
				VisualStateManager.GoToState(this, s_IndeterminateErrorStateName, true);
			}
			else if (showError)
			{
				VisualStateManager.GoToState(this, s_ErrorStateName, true);
			}
			else if (isIndeterminate && ShowPaused)
			{
				VisualStateManager.GoToState(this, s_IndeterminatePausedStateName, true);
			}
			else if (ShowPaused)
			{
				VisualStateManager.GoToState(this, s_PausedStateName, true);
			}
			else if (isIndeterminate)
			{
				UpdateWidthBasedTemplateSettings();
				VisualStateManager.GoToState(this, s_IndeterminateStateName, true);
			}
			else if (!isIndeterminate)
			{
				VisualStateManager.GoToState(this, s_DeterminateStateName, true);
			}
		}

		private void SetProgressBarIndicatorWidth()
		{
			var templateSettings = TemplateSettings;

			var progressBar = m_layoutRoot;
			m_previousMeasuredWidths = new Size(double.NaN, double.NaN);

			if (progressBar != null)
			{
				var determinateProgressBarIndicator = m_determinateProgressBarIndicator;
				if (determinateProgressBarIndicator != null)
				{
					var progressBarWidth = progressBar.ActualWidth;
					var prevIndicatorWidth = determinateProgressBarIndicator.ActualWidth;
					var maximum = Maximum;
					var minimum = Minimum;
					var padding = Padding;

					m_previousMeasuredWidths = new Size(progressBarWidth, prevIndicatorWidth);

					// Adds "Updating" state in between to trigger RepositionThemeAnimation Visual Transition
					// in ProgressBar.xaml when reverting back to previous state
					VisualStateManager.GoToState(this, s_UpdatingStateName, true);

					if (IsIndeterminate)
					{
						determinateProgressBarIndicator.Width = 0;

						var indeterminateProgressBarIndicator = m_indeterminateProgressBarIndicator;
						if (indeterminateProgressBarIndicator != null)
						{
							indeterminateProgressBarIndicator.Width = progressBarWidth * 0.4; // 40% of ProgressBar Width
						}

						var indeterminateProgressBarIndicator2 = m_indeterminateProgressBarIndicator2;
						if (indeterminateProgressBarIndicator2 != null)
						{
							indeterminateProgressBarIndicator2.Width = progressBarWidth * 0.6; // 60% of ProgressBar Width
						}
					}
					else if (Math.Abs(maximum - minimum) > double.Epsilon)
					{
						var maxIndicatorWidth = progressBarWidth - (padding.Left + padding.Right);
						var increment = maxIndicatorWidth / (maximum - minimum);
						var indicatorWidth = increment * (Value - minimum);
						var widthDelta = indicatorWidth - prevIndicatorWidth;
						templateSettings.IndicatorLengthDelta = -widthDelta;
						determinateProgressBarIndicator.Width = indicatorWidth;
					}
					else
					{
						determinateProgressBarIndicator.Width = 0; // Error
					}

					UpdateStates(); // Reverts back to previous state
				}
			}
		}

		private void UpdateWidthBasedTemplateSettings()
		{
			var templateSettings = TemplateSettings;

			var progressBar = m_layoutRoot;

			var (width, height) = progressBar != null
				? (progressBar.ActualWidth, progressBar.ActualHeight)
				: (0d, 0d);

			var indeterminateProgressBarIndicatorWidth = width * 0.4; // Indicator width at 40% of ProgressBar
			var indeterminateProgressBarIndicatorWidth2 = width * 0.6; // Indicator width at 60% of ProgressBar

			templateSettings.ContainerAnimationStartPosition = indeterminateProgressBarIndicatorWidth * -1.0; // Position at -100%
			templateSettings.ContainerAnimationEndPosition = indeterminateProgressBarIndicatorWidth * 3.0; // Position at 300%

			templateSettings.ContainerAnimationStartPosition2 = indeterminateProgressBarIndicatorWidth2 * -1.5; // Position at -150%
			templateSettings.ContainerAnimationEndPosition2 = indeterminateProgressBarIndicatorWidth2 * 1.66; // Position at 166%

			templateSettings.ContainerAnimationMidPosition = width * 0.2;

			var padding = Padding;
			templateSettings.ClipRect = new RectangleGeometry
			{
				Rect = new Rect(padding.Left, padding.Top, width - (padding.Right + padding.Left), height - (padding.Bottom + padding.Top))
			};
		}
	}
}
