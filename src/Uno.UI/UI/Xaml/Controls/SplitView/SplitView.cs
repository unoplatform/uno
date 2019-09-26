using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;
using Uno.UI.DataBinding;
#if XAMARIN_IOS
using View = UIKit.UIView;
#elif XAMARIN_ANDROID
using View = Android.Views.View;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Content")]
	public partial class SplitView : Control
	{
		public event TypedEventHandler<SplitView, object> PaneClosed;
		public event TypedEventHandler<SplitView, SplitViewPaneClosingEventArgs> PaneClosing;
		public event TypedEventHandler<SplitView, object> PaneOpened;
		public event TypedEventHandler<SplitView, object> PaneOpening;

		private CompositeDisposable _subscriptions;
		private readonly SerialDisposable _runningSubscription = new SerialDisposable();
		private Button _lightDismissLayer;
		private bool _isViewReady;

		public SplitView()
		{
			DefaultStyleKey = typeof(SplitView);
		}

#if XAMARIN_IOS
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

		public static readonly DependencyProperty CompactPaneLengthProperty =
			DependencyProperty.Register(
				"CompactPaneLength",
				typeof(double), typeof(SplitView),
				new PropertyMetadata(
					(double)48,
					(s, e) => ((SplitView)s)?.OnCompactPaneLengthChanged(e)
				)
			);

		private void OnCompactPaneLengthChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateTemplateSettings();
		}

		#endregion

		#region Content DependencyProperty

		public View Content
		{
			get { return (View)this.GetValue(ContentProperty); }
			set { this.SetValue(ContentProperty, value); }
		}

		public static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register(
				"Content",
				typeof(View),
				typeof(SplitView),
				new PropertyMetadata(
					null,
					(s, e) => ((SplitView)s)?.OnContentChanged(e)
				)
			);

		private void OnContentChanged(DependencyPropertyChangedEventArgs e)
		{
			SynchronizeContentTemplatedParent();
		}

		#endregion

		#region Pane DependencyProperty

		public View Pane
		{
			get { return (View)this.GetValue(PaneProperty); }
			set { this.SetValue(PaneProperty, value); }
		}

		public static readonly DependencyProperty PaneProperty =
			DependencyProperty.Register(
				"Pane",
				typeof(View),
				typeof(SplitView),
				new PropertyMetadata(
					null,
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

		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register(
				"DisplayMode",
				typeof(SplitViewDisplayMode),
				typeof(SplitView),
				new PropertyMetadata(
					SplitViewDisplayMode.Overlay,
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
		public static readonly DependencyProperty IsPaneOpenProperty =
			DependencyProperty.Register(
				"IsPaneOpen",
				typeof(bool),
				typeof(SplitView),
				new PropertyMetadata(
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

		public static readonly DependencyProperty OpenPaneLengthProperty =
			DependencyProperty.Register(
				"OpenPaneLength",
				typeof(double),
				typeof(SplitView),
				new PropertyMetadata(
					(double)320,
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

		public static readonly DependencyProperty PaneBackgroundProperty =
			DependencyProperty.Register(
				"PaneBackground",
				typeof(Brush),
				typeof(SplitView),
				new PropertyMetadata(
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

		public static readonly DependencyProperty PanePlacementProperty =
			DependencyProperty.Register(
				"PanePlacement",
				typeof(SplitViewPanePlacement),
				typeof(SplitView),
				new PropertyMetadata(
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

		public static readonly DependencyProperty TemplateSettingsProperty =
			DependencyProperty.Register(
				"TemplateSettings",
				typeof(SplitViewTemplateSettings),
				typeof(SplitView),
				new PropertyMetadata(
					new SplitViewTemplateSettings(null),
					(s, e) => ((SplitView)s)?.OnTemplateSettingsPropertyChanged(e)
				)
			);

		private void OnTemplateSettingsPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UpdateTemplateSettings();

			_lightDismissLayer = FindName("LightDismissLayer") as Button;

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

		protected override void OnLoaded()
		{
			base.OnLoaded();

			_subscriptions = new CompositeDisposable();

			_runningSubscription.Disposable = _subscriptions;

			UpdateControl();

			SynchronizeContentTemplatedParent();
		}

		protected override void OnUnloaded()
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
				RoutedEventHandler handler = (s, e) => IsPaneOpen = false;

				_lightDismissLayer.Click += handler;

				_subscriptions.Add(() => _lightDismissLayer.Click -= handler);
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

			Dispatcher.RunAsync(
				Core.CoreDispatcherPriority.Normal,
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
