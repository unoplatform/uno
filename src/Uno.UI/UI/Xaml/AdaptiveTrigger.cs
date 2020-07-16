using System;
using Uno.Disposables;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

namespace Windows.UI.Xaml
{
	public partial class AdaptiveTrigger : StateTriggerBase
	{
		private readonly SerialDisposable _sizeChangedSubscription = new SerialDisposable();

		public AdaptiveTrigger()
		{
			UpdateState();
		}

		private void OnCurrentWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			UpdateState();
		}

		private void UpdateState()
		{
			var size = Window.Current.Bounds;

			var w = size.Width;
			var h = size.Height;
			var mw = MinWindowWidth;
			var mh = MinWindowHeight;

			var isActive = w >= mw && h >= mh;

			if (isActive && mw >= 0)
			{
				// If we have 'mw' and 'mh' we activate using the 'MinWidthTrigger' as it's higher ranked by 'ViusalStateGroup.GetActiveTrigger()'
				SetActivePrecedence(StateTriggerPrecedence.MinWidthTrigger);
			}
			else if (isActive)
			{
				// We don't validate that 'mh > 0' so we are able to activate trigger with 'MinWindowWidth = 0' and 'MinWindowHeight = 0'
				SetActivePrecedence(StateTriggerPrecedence.MinHeightTrigger);
			}
			else
			{
				SetActivePrecedence(StateTriggerPrecedence.Inactive);
			}
		}

		#region MinWindowHeight DependencyProperty

		public double MinWindowHeight
		{
			get => (double)this.GetValue(MinWindowHeightProperty);
			set => this.SetValue(MinWindowHeightProperty, value);
		}

		// Using a DependencyProperty as the backing store for MinWindowHeight.  This enables animation, styling, binding, etc...
		public static DependencyProperty MinWindowHeightProperty { get ; } =
			DependencyProperty.Register("MinWindowHeight", typeof(double), typeof(AdaptiveTrigger), new FrameworkPropertyMetadata(-1d, (s, e) => ((AdaptiveTrigger)s)?.OnMinWindowHeightChanged(e)));

		private void OnMinWindowHeightChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateState();
		}

		#endregion

		#region MinWindowWidth DependencyProperty

		public double MinWindowWidth
		{
			get => (double)GetValue(MinWindowWidthProperty);
			set => SetValue(MinWindowWidthProperty, value);
		}

		// Using a DependencyProperty as the backing store for MinWindowWidthProperty.  This enables animation, styling, binding, etc...
		public static DependencyProperty MinWindowWidthProperty { get ; } =
			DependencyProperty.Register("MinWindowWidthProperty", typeof(double), typeof(AdaptiveTrigger), new FrameworkPropertyMetadata(-1d, (s, e) => ((AdaptiveTrigger)s)?.OnMinWindowWidthChanged(e)));


		private void OnMinWindowWidthChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateState();
		}

		#endregion

		internal override void OnOwnerChanged()
		{
			base.OnOwnerChanged();

			_sizeChangedSubscription.Disposable = null;

			if (Owner != null)
			{
				_sizeChangedSubscription.Disposable = Window.Current.RegisterSizeChangedEvent(OnCurrentWindowSizeChanged);
			}
		}
	}
}
