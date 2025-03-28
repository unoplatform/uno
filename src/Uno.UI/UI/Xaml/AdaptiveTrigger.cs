using Uno.Disposables;

#if HAS_UNO_WINUI
using WindowSizeChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.WindowSizeChangedEventArgs;
#else
using WindowSizeChangedEventArgs = Windows.UI.Core.WindowSizeChangedEventArgs;
#endif

namespace Windows.UI.Xaml
{
	public partial class AdaptiveTrigger : StateTriggerBase
	{
		private readonly SerialDisposable _sizeChangedSubscription = new SerialDisposable();

		public AdaptiveTrigger()
		{
		}

		public XamlRoot XamlRoot => XamlRoot.GetForElement(this);

		private void OnXamlRootChanged(object sender, XamlRootChangedEventArgs e) => UpdateState();

		private void UpdateState()
		{
			if (XamlRoot is not { } xamlRoot)
			{
				return;
			}

			var size = xamlRoot.Bounds;

			var w = size.Width;
			var h = size.Height;
			var mw = MinWindowWidth;
			var mh = MinWindowHeight;

			var isActive = w >= mw && h >= mh;

			SetActivePrecedence(isActive ? StateTriggerPrecedence.AdaptiveTrigger : StateTriggerPrecedence.Inactive);
		}

		#region MinWindowHeight DependencyProperty

		public double MinWindowHeight
		{
			get => (double)this.GetValue(MinWindowHeightProperty);
			set => this.SetValue(MinWindowHeightProperty, value);
		}

		// Using a DependencyProperty as the backing store for MinWindowHeight.  This enables animation, styling, binding, etc...
		public static DependencyProperty MinWindowHeightProperty { get; } =
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
		public static DependencyProperty MinWindowWidthProperty { get; } =
			DependencyProperty.Register("MinWindowWidthProperty", typeof(double), typeof(AdaptiveTrigger), new FrameworkPropertyMetadata(-1d, (s, e) => ((AdaptiveTrigger)s)?.OnMinWindowWidthChanged(e)));


		private void OnMinWindowWidthChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateState();
		}

		#endregion

		internal override void OnOwnerChanged()
		{
			base.OnOwnerChanged();

			DetachSizeChanged();
			AttachSizeChanged();
		}

		internal override void OnOwnerElementChanged()
		{
			base.OnOwnerElementChanged();

			DetachSizeChanged();
			AttachSizeChanged();
		}

		internal override void OnOwnerElementLoaded()
		{
			base.OnOwnerElementLoaded();

			AttachSizeChanged();
		}

		internal override void OnOwnerElementUnloaded()
		{
			base.OnOwnerElementUnloaded();

			DetachSizeChanged();
		}

		private void AttachSizeChanged()
		{
			if (XamlRoot is { } xamlRoot)
			{
				xamlRoot.Changed += OnXamlRootChanged;
				UpdateState();

				_sizeChangedSubscription.Disposable = Disposable.Create(() => xamlRoot.Changed -= OnXamlRootChanged);
			}
		}

		private void DetachSizeChanged() => _sizeChangedSubscription.Disposable = null;
	}
}
