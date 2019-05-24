using System;
using Uno.Disposables;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml
{
	public partial class AdaptiveTrigger : StateTriggerBase
	{
		private readonly SerialDisposable _sizeChangedSubscription = new SerialDisposable();

		public AdaptiveTrigger()
		{
			UpdateState();
		}

		private void OnCurrentWindowSizeChanged(object sender, Core.WindowSizeChangedEventArgs e)
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

			if (mw >= 0 && w >= mw)
			{
				SetActivePrecedence(StateTriggerPrecedence.MinWidthTrigger);
			}
			else if (mh >= 0 && h > mh)
			{
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
		public static readonly DependencyProperty MinWindowHeightProperty =
			DependencyProperty.Register("MinWindowHeight", typeof(double), typeof(AdaptiveTrigger), new PropertyMetadata(-1d, (s, e) => ((AdaptiveTrigger)s)?.OnMinWindowHeightChanged(e)));

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
		public static readonly DependencyProperty MinWindowWidthProperty =
			DependencyProperty.Register("MinWindowWidthProperty", typeof(double), typeof(AdaptiveTrigger), new PropertyMetadata(-1d, (s, e) => ((AdaptiveTrigger)s)?.OnMinWindowWidthChanged(e)));


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
