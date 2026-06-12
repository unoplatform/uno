using System;
using System.ComponentModel;
using Uno.Disposables;
using Windows.Foundation;

using WindowSizeChangedEventArgs = Microsoft.UI.Xaml.WindowSizeChangedEventArgs;

namespace Microsoft.UI.Xaml
{
	public partial class AdaptiveTrigger : StateTriggerBase
	{
		private readonly SerialDisposable _sizeChangedSubscription = new SerialDisposable();

		private static Size? _windowSizeOverride;

		// Raised when the global window-size override set through SetWindowSizeOverride changes, so that
		// every live AdaptiveTrigger re-evaluates its active state (the same role XamlRoot.Changed plays).
		private static event EventHandler WindowSizeOverrideChanged;

		public AdaptiveTrigger()
		{
		}

		public XamlRoot XamlRoot => XamlRoot.GetForElement(this);

		/// <summary>
		/// Overrides the size that every <see cref="AdaptiveTrigger"/> uses to evaluate its active state,
		/// instead of the hosting window's <see cref="Microsoft.UI.Xaml.XamlRoot"/> bounds. Passing
		/// <c>null</c> reverts to the default behavior (the window bounds).
		/// </summary>
		/// <remarks>
		/// This is an Uno-specific host-extensibility hook with no WinUI equivalent. It mirrors the Uno
		/// Toolkit's <c>ResponsiveHelper.SetOverrideSizeProvider</c>, so a host that simulates a form factor
		/// by resizing the previewed content — rather than the window — can make platform adaptive triggers
		/// re-evaluate against that simulated size. The override is global, matching the Toolkit's own
		/// global responsive size source.
		/// </remarks>
		/// <param name="size">The simulated window size, or <c>null</c> to clear the override.</param>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void SetWindowSizeOverride(Size? size)
		{
			_windowSizeOverride = size;
			WindowSizeOverrideChanged?.Invoke(null, EventArgs.Empty);
		}

		private void OnXamlRootChanged(object sender, XamlRootChangedEventArgs e) => UpdateState();

		private void OnWindowSizeOverrideChanged(object sender, EventArgs e) => UpdateState();

		private void UpdateState()
		{
			double w, h;
			if (_windowSizeOverride is { } overrideSize)
			{
				w = overrideSize.Width;
				h = overrideSize.Height;
			}
			else if (XamlRoot is { } xamlRoot)
			{
				var bounds = xamlRoot.Bounds;
				w = bounds.Width;
				h = bounds.Height;
			}
			else
			{
				return;
			}

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
			var subscriptions = new CompositeDisposable();

			if (XamlRoot is { } xamlRoot)
			{
				xamlRoot.Changed += OnXamlRootChanged;
				subscriptions.Add(Disposable.Create(() => xamlRoot.Changed -= OnXamlRootChanged));
			}

			// Always listen for the host size override so the trigger re-evaluates against the simulated
			// size even when XamlRoot.Bounds (the real window) never changes.
			WindowSizeOverrideChanged += OnWindowSizeOverrideChanged;
			subscriptions.Add(Disposable.Create(() => WindowSizeOverrideChanged -= OnWindowSizeOverrideChanged));

			_sizeChangedSubscription.Disposable = subscriptions;

			UpdateState();
		}

		private void DetachSizeChanged() => _sizeChangedSubscription.Disposable = null;
	}
}
