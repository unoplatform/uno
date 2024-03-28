using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Content))]
	public partial class SplitView : Control
	{
		public event TypedEventHandler<SplitView, object> PaneClosed;
		public event TypedEventHandler<SplitView, SplitViewPaneClosingEventArgs> PaneClosing;
		public event TypedEventHandler<SplitView, object> PaneOpened;
		public event TypedEventHandler<SplitView, object> PaneOpening;

		private CompositeDisposable _subscriptions;
		private readonly SerialDisposable _runningSubscription = new SerialDisposable();
		private FrameworkElement _lightDismissLayer;
		private bool _isViewReady;

		public SplitView()
		{
			DefaultStyleKey = typeof(SplitView);
		}

#if __IOS__
		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			TemplateSettings.ViewHeight = Bounds.Height;
		}
#endif

		#region CompactPaneLength DependencyProperty

		public double CompactPaneLength
		{
			get { return (double)this.GetValue(CompactPaneLengthProperty); }
			set { this.SetValue(CompactPaneLengthProperty, value); }
		}

		public static DependencyProperty CompactPaneLengthProperty { get; } =
			DependencyProperty.Register(
				"CompactPaneLength",
				typeof(double), typeof(SplitView),
				new FrameworkPropertyMetadata(
					defaultValue: (double)48,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					(s, e) => ((SplitView)s)?.OnCompactPaneLengthChanged(e)
				)
			);

		private void OnCompactPaneLengthChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateTemplateSettings();
		}

		#endregion

		#region Content DependencyProperty

		public UIElement Content
		{
			get { return (UIElement)this.GetValue(ContentProperty); }
			set { this.SetValue(ContentProperty, value); }
		}

		public static DependencyProperty ContentProperty { get; } =
			DependencyProperty.Register(
				"Content",
				typeof(UIElement),
				typeof(SplitView),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					(s, e) => ((SplitView)s)?.OnContentChanged(e)
				)
			);

		private void OnContentChanged(DependencyPropertyChangedEventArgs e)
		{
			SynchronizeContentTemplatedParent();
		}

		#endregion

		#region Pane DependencyProperty

		public UIElement Pane
		{
			get { return (UIElement)this.GetValue(PaneProperty); }
			set { this.SetValue(PaneProperty, value); }
		}

		public static DependencyProperty PaneProperty { get; } =
			DependencyProperty.Register(
				"Pane",
				typeof(UIElement),
				typeof(SplitView),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					(s, e) => ((SplitView)s)?.OnPaneChanged(e)
				)
			);

		private void OnPaneChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region DisplayMode DependencyProperty

		public SplitViewDisplayMode DisplayMode
		{
			get { return (SplitViewDisplayMode)this.GetValue(DisplayModeProperty); }
			set { this.SetValue(DisplayModeProperty, value); }
		}

		public static DependencyProperty DisplayModeProperty { get; } =
			DependencyProperty.Register(
				"DisplayMode",
				typeof(SplitViewDisplayMode),
				typeof(SplitView),
				new FrameworkPropertyMetadata(
					defaultValue: SplitViewDisplayMode.Overlay,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					(s, e) => ((SplitView)s)?.OnDisplayModeChanged(e)
				)
			);

		private void OnDisplayModeChanged(DependencyPropertyChangedEventArgs e)
		{
			SetNeedsUpdateVisualStates();
		}

		#endregion

		#region IsPaneOpen DependencyProperty

		public bool IsPaneOpen
		{
			get { return (bool)this.GetValue(IsPaneOpenProperty); }
			set { this.SetValue(IsPaneOpenProperty, value); }
		}

		//There is an error in the MSDN docs saying that the default value for IsPaneOpen is true, it is actually false
		public static DependencyProperty IsPaneOpenProperty { get; } =
			DependencyProperty.Register(
				"IsPaneOpen",
				typeof(bool),
				typeof(SplitView),
				new FrameworkPropertyMetadata(
					false,

					(s, e) => ((SplitView)s)?.OnIsPaneOpenChanged(e)
				)
			);

		private void OnIsPaneOpenChanged(DependencyPropertyChangedEventArgs e)
		{
			SetNeedsUpdateVisualStates();
		}

		#endregion

		#region OpenPaneLength DependencyProperty

		public double OpenPaneLength
		{
			get { return (double)this.GetValue(OpenPaneLengthProperty); }
			set { this.SetValue(OpenPaneLengthProperty, value); }
		}

		public static DependencyProperty OpenPaneLengthProperty { get; } =
			DependencyProperty.Register(
				"OpenPaneLength",
				typeof(double),
				typeof(SplitView),
				new FrameworkPropertyMetadata(
					defaultValue: (double)320,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					(s, e) => ((SplitView)s)?.OnOpenPaneLengthChanged(e)
				)
			);

		private void OnOpenPaneLengthChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateTemplateSettings();
		}

		#endregion

		#region PaneBackground DependencyProperty

		public Brush PaneBackground
		{
			get { return (Brush)this.GetValue(PaneBackgroundProperty); }
			set { this.SetValue(PaneBackgroundProperty, value); }
		}

		public static DependencyProperty PaneBackgroundProperty { get; } =
			DependencyProperty.Register(
				"PaneBackground",
				typeof(Brush),
				typeof(SplitView),
				new FrameworkPropertyMetadata(
					SolidColorBrushHelper.Transparent,
					(s, e) => ((SplitView)s)?.OnPaneBackgroundChanged(e)
				)
			);

		private void OnPaneBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region PanePlacement DependencyProperty

		public SplitViewPanePlacement PanePlacement
		{
			get { return (SplitViewPanePlacement)this.GetValue(PanePlacementProperty); }
			set { this.SetValue(PanePlacementProperty, value); }
		}

		public static DependencyProperty PanePlacementProperty { get; } =
			DependencyProperty.Register(
				"PanePlacement",
				typeof(SplitViewPanePlacement),
				typeof(SplitView),
				new FrameworkPropertyMetadata(
					SplitViewPanePlacement.Left,
					(s, e) => ((SplitView)s)?.OnPanePlacementChanged(e)
				)
			);

		private void OnPanePlacementChanged(DependencyPropertyChangedEventArgs e)
		{
			SetNeedsUpdateVisualStates();
		}

		#endregion

		#region TemplateSettingsProperty DependencyProperty

		public SplitViewTemplateSettings TemplateSettings
		{
			get { return (SplitViewTemplateSettings)this.GetValue(TemplateSettingsProperty); }
			private set { this.SetValue(TemplateSettingsProperty, value); }
		}

		public static DependencyProperty TemplateSettingsProperty { get; } =
			DependencyProperty.Register(
				"TemplateSettings",
				typeof(SplitViewTemplateSettings),
				typeof(SplitView),
				new FrameworkPropertyMetadata(
					new SplitViewTemplateSettings(null),
					(s, e) => ((SplitView)s)?.OnTemplateSettingsPropertyChanged(e)
				)
			);

		private void OnTemplateSettingsPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UpdateTemplateSettings();

			_lightDismissLayer = FindName("LightDismissLayer") as FrameworkElement;

			_isViewReady = true;

			UpdateControl();

			SetNeedsUpdateVisualStates();
		}

		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			// This is required to ensure that FrameworkElement.FindName can dig through the tree after
			// the control has been created.
			SynchronizeContentTemplatedParent();
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			_subscriptions = new CompositeDisposable();

			_runningSubscription.Disposable = _subscriptions;

			UpdateControl();

			SynchronizeContentTemplatedParent();
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_runningSubscription.Disposable = null;
		}

		private void SynchronizeContentTemplatedParent()
		{
			// Manual propagation of the templated parent to the content property
			// until we get the propagation running properly
			if (Content is IFrameworkElement contentBinder)
			{
				contentBinder.TemplatedParent = this.TemplatedParent;
			}
			if (Pane is IFrameworkElement paneBinder)
			{
				paneBinder.TemplatedParent = this.TemplatedParent;
			}
		}

		private void UpdateControl()
		{
			if (_isViewReady && _runningSubscription.Disposable != null)
			{
				RegisterEvents();
			}
		}

		private void RegisterEvents()
		{
			if (_lightDismissLayer != null)
			{
				if (_lightDismissLayer is ButtonBase button)
				{
					// PointerReleased isn't raised for buttons
					RoutedEventHandler handler = (s, e) => IsPaneOpen = false;
					button.Click += handler;
					_subscriptions.Add(() => button.Click -= handler);
				}
				else
				{
					PointerEventHandler handler = (s, e) => IsPaneOpen = false;
					_lightDismissLayer.PointerReleased += handler;
					_subscriptions.Add(() => _lightDismissLayer.PointerReleased -= handler);
				}
			}
		}

		private void UpdateTemplateSettings()
		{
			this.TemplateSettings = new SplitViewTemplateSettings(this);
		}

		/// <summary>
		/// DisplayModeStates
		/// -----------------
		/// Closed
		/// ClosedCompactLeft
		/// ClosedCompactRight
		/// OpenOverlayLeft
		/// OpenOverlayRight
		/// OpenInlineLeft
		/// OpenInlineRight
		/// OpenCompactOverlayLeft
		/// OpenCompactOverlayRight
		/// </summary>
		private void UpdateVisualStates(bool useTransitons)
		{
			string stateName = GetStateName();

			if (!IsPaneOpen)
			{
				PaneClosing?.Invoke(this, new SplitViewPaneClosingEventArgs());
			}
			else
			{
				PaneOpening?.Invoke(this, null);
			}

#if __IOS__
			PatchInvalidFinalState(stateName);
#endif
			VisualStateManager.GoToState(this, stateName, useTransitons);

			if (!IsPaneOpen)
			{
				PaneClosed?.Invoke(this, null);
			}
			else
			{
				PaneOpened?.Invoke(this, null);
			}
		}

		private bool _needsVisualStateUpdate;
		private void SetNeedsUpdateVisualStates()
		{
			if (_needsVisualStateUpdate)
			{
				return;
			}
			_needsVisualStateUpdate = true;

			_ = Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() =>
				{
					UpdateVisualStates(true);
					_needsVisualStateUpdate = false;
				}
			);
		}

		private string GetStateName()
		{
			var openState = this.IsPaneOpen ? "Open" : "Closed";

			var side = string.Empty;
			switch (PanePlacement)
			{
				case SplitViewPanePlacement.Left:
					side = "Left";
					break;
				case SplitViewPanePlacement.Right:
					side = "Right";
					break;
			}

			var displayMode = string.Empty;
			switch (DisplayMode)
			{
				case SplitViewDisplayMode.Overlay:
					displayMode = IsPaneOpen ? "Overlay" : string.Empty;
					break;
				case SplitViewDisplayMode.Inline:
					displayMode = IsPaneOpen ? "Inline" : string.Empty;
					break;
				case SplitViewDisplayMode.CompactOverlay:
					displayMode = IsPaneOpen ? "CompactOverlay" : "Compact";
					break;
				case SplitViewDisplayMode.CompactInline:
					displayMode = IsPaneOpen ? "Inline" : "Compact";
					break;
			}

			if (openState == "Closed" && displayMode.IsNullOrWhiteSpace())
			{
				side = "";
			}

			return $"{openState}{displayMode}{side}";
		}
	}
}
