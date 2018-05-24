using System;
using Uno.Disposables;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml
{
	public partial class AdaptiveTrigger : StateTriggerBase
	{
		private SerialDisposable _sizeChangedSubscription = new SerialDisposable();

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

			var isMinWidthSet = MinWindowWidth != -1;
			var isMinHeightSet = MinWindowHeight != -1;

			var widthIsActive = isMinWidthSet && size.Width >= MinWindowWidth;
			var heightIsActive = isMinHeightSet && size.Height >= MinWindowHeight;

			SetActive(
				(isMinWidthSet && isMinHeightSet && heightIsActive && widthIsActive)
				|| (isMinWidthSet && widthIsActive)
				|| (isMinHeightSet && heightIsActive)
			);
		}

		#region MinWindowHeight DependencyProperty

		public double MinWindowHeight
		{
			get { return (double)this.GetValue(MinWindowHeightProperty); }
			set { this.SetValue(MinWindowHeightProperty, value); }
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
			get { return (double)this.GetValue(MinWindowWidthProperty); }
			set { this.SetValue(MinWindowWidthProperty, value); }
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
