using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class ProgressBar : RangeBase
	{
		private FrameworkElement _determinateRoot;
		private FrameworkElement _progressBarIndicator;

		static ProgressBar()
		{
			MaximumProperty.OverrideMetadata(typeof(ProgressBar), new FrameworkPropertyMetadata(100d));
		}

		public ProgressBar()
		{
			TemplateSettings = new ProgressBarTemplateSettings();

			DefaultStyleKey = typeof(ProgressBar);
		}

		public global::Windows.UI.Xaml.Controls.Primitives.ProgressBarTemplateSettings TemplateSettings
		{
			get;
		}


		#region IsIndeterminate

		public bool IsIndeterminate
		{
			get { return (bool)this.GetValue(IsIndeterminateProperty); }
			set { this.SetValue(IsIndeterminateProperty, value); }
		}

		public static DependencyProperty IsIndeterminateProperty { get; } =
			DependencyProperty.Register(
				"IsIndeterminate",
				typeof(bool),
				typeof(ProgressBar),
				new FrameworkPropertyMetadata(
					false,
					(s, e) => (s as ProgressBar)?.OnIsIndeterminateChanged((bool)e.OldValue, (bool)e.NewValue)
				)
			);

		protected virtual void OnIsIndeterminateChanged(bool oldValue, bool newValue)
		{
			UpdateCommonStates();

			OnIsIndeterminateChangedPartial(oldValue, newValue);
		}

		partial void OnIsIndeterminateChangedPartial(bool oldValue, bool newValue);

		#endregion

		#region ShowError

		public bool ShowError
		{
			get { return (bool)this.GetValue(ShowErrorProperty); }
			set { this.SetValue(ShowErrorProperty, value); }
		}

		public static DependencyProperty ShowErrorProperty { get; } =
			DependencyProperty.Register(
				"ShowError",
				typeof(bool),
				typeof(ProgressBar),
				new FrameworkPropertyMetadata(
					false,
					(s, e) => (s as ProgressBar)?.OnShowErrorChanged((bool)e.OldValue, (bool)e.NewValue)
				)
			);

		protected virtual void OnShowErrorChanged(bool oldValue, bool newValue)
		{
			UpdateCommonStates();
		}

		#endregion

		#region ShowPaused

		public bool ShowPaused
		{
			get { return (bool)this.GetValue(ShowPausedProperty); }
			set { this.SetValue(ShowPausedProperty, value); }
		}

		public static DependencyProperty ShowPausedProperty { get; } =
			DependencyProperty.Register(
				"ShowPaused",
				typeof(bool),
				typeof(ProgressBar),
				new FrameworkPropertyMetadata(
					false,
					(s, e) => (s as ProgressBar)?.OnShowPausedChanged((bool)e.OldValue, (bool)e.NewValue)
				)
			);

		protected virtual void OnShowPausedChanged(bool oldValue, bool newValue)
		{
			UpdateCommonStates();
		}

		#endregion

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_determinateRoot = GetTemplateChild("DeterminateRoot") as FrameworkElement;
			_progressBarIndicator = GetTemplateChild("ProgressBarIndicator") as FrameworkElement;

			if (_determinateRoot != null)
			{
				_determinateRoot.SizeChanged += (s, e) =>
				{
					UpdateProgress(dispatchInvalidate: true);
				};
			}

			UpdateProgress();
		}

		protected override void OnValueChanged(double oldValue, double newValue)
		{
			base.OnValueChanged(oldValue, newValue);
			UpdateProgress();
		}

		protected override void OnMaximumChanged(double oldValue, double newValue)
		{
			base.OnMaximumChanged(oldValue, newValue);
			UpdateProgress();
		}

		protected override void OnMinimumChanged(double oldValue, double newValue)
		{
			base.OnMinimumChanged(oldValue, newValue);
			UpdateProgress();
		}

		private void UpdateProgress(bool dispatchInvalidate = false)
		{
			if (_progressBarIndicator != null && _determinateRoot != null)
			{
				_progressBarIndicator.Width = _determinateRoot.ActualWidth * (Value / (Maximum - Minimum));

				if (dispatchInvalidate)
				{
#if __ANDROID__
					// Changing _progressBarIndicator.Width while in a layout phase doesn't reliably trigger a relayout on Android.
					// Dispatching InvalidateMeasure appears to solve that problem.
					_ = Dispatcher.RunAnimation(() => _progressBarIndicator.InvalidateMeasure());
#endif
				}
			}
		}

		private void UpdateCommonStates()
		{
			if (IsIndeterminate)
			{
				VisualStateManager.GoToState(this, "Indeterminate", false);
			}
			else if (ShowError)
			{
				VisualStateManager.GoToState(this, "Error", false);
			}
			else if (ShowPaused)
			{
				VisualStateManager.GoToState(this, "Paused", false);
			}
			else if (!IsIndeterminate)
			{
				VisualStateManager.GoToState(this, "Determinate", false);
			}
			else // if (Updating)
			{
				VisualStateManager.GoToState(this, "Updating", false);
			}
		}

		// TODO
		// public ProgressBarTemplateSettings TemplateSettings { get; }
	}
}
