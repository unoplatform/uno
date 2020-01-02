using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.UI;
using Uno.UI.DataBinding;
using System.Linq;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Windows.UI.Xaml.Markup;
using System.ComponentModel;
using System.Reflection;
using Uno.UI.Xaml;
#if XAMARIN_ANDROID
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using UIKit;
#elif __MACOS__
using View = AppKit.NSView;
using ViewGroup = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
using AppKit;
#elif __WASM__ || NET461
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Control : FrameworkElement
	{
		private View _templatedRoot;
		private bool _updateTemplate;

		private void InitializeControl()
		{
			SetDefaultForeground();
			SubscribeToOverridenRoutedEvents();
			OnIsFocusableChanged();
		}

		/// <summary>
		/// This property is not used in Uno.UI, and is always set to the current top-level type.
		/// </summary>
		protected object DefaultStyleKey { get; set; }

		protected override bool IsSimpleLayout => true;

		private void SetDefaultForeground()
		{
			//override the default value from dependency property based on application theme
			//in the future, this will need to respond to the inherited RequestedTheme and its changes
			this.SetValue(ForegroundProperty,
				Application.Current == null || Application.Current.RequestedTheme == ApplicationTheme.Light
					? SolidColorBrushHelper.Black
					: SolidColorBrushHelper.White, DependencyPropertyValuePrecedences.DefaultValue);
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// this is defined in the FrameworkElement mixin, and must not be used in Control.
			// When setting the background color in a Control, the property is simply used as a placeholder
			// for children controls, applied by inheritance. 

			// base.OnBackgroundChanged(e);
		}

		/// <summary>
		/// Will be set to Template when it is applied
		/// </summary>
		private ControlTemplate _controlTemplateUsedLastUpdate;

		partial void UnregisterSubView();
		partial void RegisterSubView(View child);


		#region Template DependencyProperty

		public ControlTemplate Template
		{
			get { return (ControlTemplate)GetValue(TemplateProperty); }
			set { SetValue(TemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Template.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TemplateProperty =
			DependencyProperty.Register("Template", typeof(ControlTemplate), typeof(Control), new PropertyMetadata(null, (s, e) => ((Control)s)?.OnTemplateChanged(e)));

		private void OnTemplateChanged(DependencyPropertyChangedEventArgs e)
		{
			_updateTemplate = true;
			SetUpdateControlTemplate();
		}

		#endregion

		/// <summary>
		/// Defines a method that will request the update of the control's template and request layout update.
		/// </summary>
		/// <param name="forceUpdate">If true, forces an update even if the control has no parent.</param>
		internal void SetUpdateControlTemplate(bool forceUpdate = false)
		{
			if (
				!FeatureConfiguration.Control.UseLegacyLazyApplyTemplate
				|| forceUpdate
				|| this.HasParent()
				|| CanCreateTemplateWithoutParent
			)
			{
				UpdateTemplate();
				this.InvalidateMeasure();
			}
		}

		/// <summary>
		/// Represents the single child that is the result of the control template application.
		/// </summary>
		internal View TemplatedRoot
		{
			get { return _templatedRoot; }
			set
			{
				CleanupView(_templatedRoot);

				UnregisterSubView();

				_templatedRoot = value;

				if (value != null)
				{
					if(_templatedRoot is IDependencyObjectStoreProvider provider)
					{
						provider.Store.SetValue(provider.Store.TemplatedParentProperty, this, DependencyPropertyValuePrecedences.Local);
					}

					RegisterSubView(value);

					if (_templatedRoot != null)
					{
						RegisterContentTemplateRoot();

						OnApplyTemplate();
					}
				}
			}
		}

		private void SubscribeToOverridenRoutedEvents()
		{
			// Overridden Events are registered from constructor to ensure they are
			// registered first in event handlers.
			// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.control.onpointerpressed#remarks

			var implementedEvents = GetImplementedRoutedEvents(GetType());

			if (implementedEvents.HasFlag(RoutedEventFlag.PointerPressed))
			{
				PointerPressed += OnPointerPressedHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.PointerReleased))
			{
				PointerReleased += OnPointerReleasedHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.PointerMoved))
			{
				PointerMoved += OnPointerMovedHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.PointerEntered))
			{
				PointerEntered += OnPointerEnteredHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.PointerExited))
			{
				PointerExited += OnPointerExitedHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.PointerCanceled))
			{
				PointerCanceled += OnPointerCanceledHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.PointerCaptureLost))
			{
				PointerCaptureLost += OnPointerCaptureLostHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.ManipulationStarting))
			{
				ManipulationStarting += OnManipulationStartingHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.ManipulationStarted))
			{
				ManipulationStarted += OnManipulationStartedHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.ManipulationDelta))
			{
				ManipulationDelta += OnManipulationDeltaHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.ManipulationInertiaStarting))
			{
				ManipulationInertiaStarting += OnManipulationInertiaStartingHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.ManipulationCompleted))
			{
				ManipulationCompleted += OnManipulationCompletedHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.Tapped))
			{
				Tapped += OnTappedHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.DoubleTapped))
			{
				DoubleTapped += OnDoubleTappedHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.KeyDown))
			{
				KeyDown += OnKeyDownHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.KeyUp))
			{
				KeyUp += OnKeyUpHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.GotFocus))
			{
				GotFocus += OnGotFocusHandler;
			}

			if (implementedEvents.HasFlag(RoutedEventFlag.LostFocus))
			{
				LostFocus += OnLostFocusHandler;
			}
		}

		protected override void OnLoaded()
		{
			SetUpdateControlTemplate();

			base.OnLoaded();

		}

		/// <summary>
		/// Loads the relevant control template so that its parts can be referenced.
		/// </summary>
		/// <returns>A value that indicates whether the visual tree was rebuilt by this call. True if the tree was rebuilt; false if the previous visual tree was retained.</returns>
		public bool ApplyTemplate()
		{
			var currentTemplateRoot = _templatedRoot;
			SetUpdateControlTemplate(forceUpdate: true);

			return currentTemplateRoot != _templatedRoot;
		}

		/// <summary>
		/// Finds a realized element in the control template
		/// </summary>
		/// <param name="e">The framework element instance</param>
		/// <param name="name">The name of the template part</param>
		public DependencyObject GetTemplateChild(string childName)
		{
			return FindNameInScope(TemplatedRoot as IFrameworkElement, childName) as DependencyObject
				?? FindName(childName);
		}

		private static object FindNameInScope(IFrameworkElement root, string name)
		{
			return root != null
				&& name != null
				&& NameScope.GetNameScope(root) is INameScope nameScope
				&& nameScope.FindName(name) is DependencyObject element
				// Doesn't currently support ElementStub (fallbacks to other FindName implementation)
				&& !(element is ElementStub) 
					? element
					: null;
		}

		private void CleanupView(View view)
		{
			if (view is IDependencyObjectStoreProvider provider)
			{
				provider.Store.Parent = null;
				provider.Store.ClearValue(provider.Store.TemplatedParentProperty, DependencyPropertyValuePrecedences.Local);
			}
		}

		/// <summary>
		/// Allows to bypass the validation of the presence of a parent.
		/// </summary>
		/// <remarks>
		/// Setting to true property allows the ContentControl to create its children even if
		/// no parent view has been set. This is used for the ListView control (and other virtualizing controls)
		/// to measure items properly. These controls set the size of the view based on the size reported
		/// immediately after the BaseAdapter.GetView method returns, but the parent still has not been set.
		/// 
		/// The Content control uses this delayed creation as an optimization technique for layout creation, when controls
		/// are created but not yet used.
		/// </remarks>
		protected virtual bool CanCreateTemplateWithoutParent { get; } = false;

		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			base.OnVisibilityChanged(oldValue, newValue);

			if (oldValue == Visibility.Collapsed && newValue == Visibility.Visible)
			{
				SetUpdateControlTemplate();
			}
			else if (oldValue == Visibility.Visible && newValue == Visibility.Collapsed)
			{
				Unfocus();
			}

			OnIsFocusableChanged();
		}

		private void UpdateTemplate()
		{
			// If TemplatedRoot is null, it must be updated even if the templates haven't changed
			if (TemplatedRoot == null)
			{
				_controlTemplateUsedLastUpdate = null;
			}

			if (
				!FeatureConfiguration.FrameworkElement.UseLegacyApplyStylePhase && 
				FeatureConfiguration.FrameworkElement.ClearPreviousOnStyleChange
			)
			{
				if (_updateTemplate && !object.Equals(Template, _controlTemplateUsedLastUpdate))
				{
					_controlTemplateUsedLastUpdate = Template;

					if (Template != null)
					{
						TemplatedRoot = Template.LoadContentCached();
					}
					else
					{
						TemplatedRoot = null;
					}

					_updateTemplate = false;
				}
			}
			else
			{
				if (Template != null && _updateTemplate && !object.Equals(Template, _controlTemplateUsedLastUpdate))
				{
					_controlTemplateUsedLastUpdate = Template;

					_updateTemplate = false;

					TemplatedRoot = Template.LoadContentCached();
				}
			}
		}

		partial void RegisterContentTemplateRoot();


		protected override void OnApplyTemplate()
		{
		}

		#region Foreground Dependency Property

		public
#if __ANDROID_23__
		new
#endif
		Brush Foreground
		{
			get { return (Brush)this.GetValue(ForegroundProperty); }
			set { this.SetValue(ForegroundProperty, value); }
		}

		public static readonly DependencyProperty ForegroundProperty =
			DependencyProperty.Register(
				"Foreground",
				typeof(Brush),
				typeof(Control),
				new FrameworkPropertyMetadata(
					SolidColorBrushHelper.Black,
					FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((Control)s)?.OnForegroundColorChanged(e.OldValue as Brush, e.NewValue as Brush)
				)
			);

		#endregion

		#region FontWeight

		public FontWeight FontWeight
		{
			get { return (FontWeight)this.GetValue(FontWeightProperty); }
			set { this.SetValue(FontWeightProperty, value); }
		}

		public static readonly DependencyProperty FontWeightProperty =
			DependencyProperty.Register(
				"FontWeight",
				typeof(FontWeight),
				typeof(Control),
				new FrameworkPropertyMetadata(
					FontWeights.Normal,
					FrameworkPropertyMetadataOptions.Inherits,
					(s, e) => ((Control)s)?.OnFontWeightChanged((FontWeight)e.OldValue, (FontWeight)e.NewValue)
				)
			);

		#endregion

		#region FontSize

		public double FontSize
		{
			get { return (double)this.GetValue(FontSizeProperty); }
			set { this.SetValue(FontSizeProperty, value); }
		}

		public static readonly DependencyProperty FontSizeProperty =
			DependencyProperty.Register(
				"FontSize",
				typeof(double),
				typeof(Control),
				new FrameworkPropertyMetadata(
					11.0,
					FrameworkPropertyMetadataOptions.Inherits,
					(s, e) => ((Control)s)?.OnFontSizeChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		#endregion

		#region FontFamily

		public FontFamily FontFamily
		{
			get { return (FontFamily)this.GetValue(FontFamilyProperty); }
			set { this.SetValue(FontFamilyProperty, value); }
		}

		public static readonly DependencyProperty FontFamilyProperty =
			DependencyProperty.Register(
				"FontFamily",
				typeof(FontFamily),
				typeof(Control),
				new FrameworkPropertyMetadata(
					FontFamily.Default,
					FrameworkPropertyMetadataOptions.Inherits,
					(s, e) => ((Control)s)?.OnFontFamilyChanged(e.OldValue as FontFamily, e.NewValue as FontFamily)
				)
			);
		#endregion

		#region FontStyle

		public FontStyle FontStyle
		{
			get { return (FontStyle)this.GetValue(FontStyleProperty); }
			set { this.SetValue(FontStyleProperty, value); }
		}

		public static readonly DependencyProperty FontStyleProperty =
			DependencyProperty.Register(
				"FontStyle",
				typeof(FontStyle),
				typeof(Control),
				new FrameworkPropertyMetadata(
					FontStyle.Normal,
					FrameworkPropertyMetadataOptions.Inherits,
					(s, e) => ((Control)s)?.OnFontStyleChanged((FontStyle)e.OldValue, (FontStyle)e.NewValue)
				)
			);
		#endregion

		#region Padding DependencyProperty

		public Thickness Padding
		{
			get { return (Thickness)GetValue(PaddingProperty); }
			set { SetValue(PaddingProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Padding.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PaddingProperty =
			DependencyProperty.Register(
				"Padding",
				typeof(Thickness),
				typeof(Control),
				new FrameworkPropertyMetadata(
					Thickness.Empty,
					FrameworkPropertyMetadataOptions.None,
					(s, e) => ((Control)s)?.OnPaddingChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
				)
			);

		#endregion

		#region BorderThickness DependencyProperty

		public Thickness BorderThickness
		{
			get { return (Thickness)GetValue(BorderThicknessProperty); }
			set { SetValue(BorderThicknessProperty, value); }
		}

		// Using a DependencyProperty as the backing store for BorderThickness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BorderThicknessProperty =
			DependencyProperty.Register(
				"BorderThickness",
				typeof(Thickness),
				typeof(Control),
				new FrameworkPropertyMetadata(
					Thickness.Empty,
					FrameworkPropertyMetadataOptions.None,
					(s, e) => ((Control)s)?.OnBorderThicknessChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
				)
			);

		#endregion

		#region BorderBrush Dependency Property

#if XAMARIN_ANDROID
		//This field is never accessed. It just exists to create a reference, because the DP causes issues with ImageBrush of the backing bitmap being prematurely garbage-collected. (Bug with ConditionalWeakTable? https://bugzilla.xamarin.com/show_bug.cgi?id=21620)
		private Brush _borderBrushStrongReference;
#endif

		public Brush BorderBrush
		{
			get { return (Brush)this.GetValue(BorderBrushProperty); }
			set
			{
				this.SetValue(BorderBrushProperty, value);

#if XAMARIN_ANDROID
				_borderBrushStrongReference = value;
#endif
			}
		}

		// Using a DependencyProperty as the backing store for BorderBrush.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BorderBrushProperty =
			DependencyProperty.Register(
				"BorderBrush",
				typeof(Brush),
				typeof(Control),
				new FrameworkPropertyMetadata(
					SolidColorBrushHelper.Transparent,
					FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((Control)s).OnBorderBrushChanged((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);

		#endregion

		#region FocusState DependencyProperty

		public FocusState FocusState
		{
			get { return (FocusState)GetValue(FocusStateProperty); }
			private set { SetValue(FocusStateProperty, value); }
		}

		public static DependencyProperty FocusStateProperty =
			DependencyProperty.Register(
				"FocusState",
				typeof(FocusState),
				typeof(Control),
				new PropertyMetadata(
					(FocusState)FocusState.Unfocused,
					(s, e) => ((Control)s)?.OnFocusStateChanged((FocusState)e.OldValue, (FocusState)e.NewValue)
				)
			);

		#endregion

		#region IsTabStop DependencyProperty

		public bool IsTabStop
		{
			get { return (bool)GetValue(IsTabStopProperty); }
			set { SetValue(IsTabStopProperty, value); }
		}

		public static DependencyProperty IsTabStopProperty =
			DependencyProperty.Register(
				"IsTabStop",
				typeof(bool),
				typeof(Control),
				new PropertyMetadata(
					(bool)true,
					(s, e) => ((Control)s)?.OnIsFocusableChanged()
				)
			);
		#endregion

		internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);
			OnDataContextChanged();
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected virtual void OnDataContextChanged()
		{
		}

		protected override void OnIsEnabledChanged(bool oldValue, bool newValue)
		{
			base.OnIsEnabledChanged(oldValue, newValue);
			OnIsFocusableChanged();
		}

		partial void OnIsFocusableChanged();
		internal bool IsFocusable =>
			Visibility == Visibility.Visible
			&& IsEnabled
			&& IsTabStop;

		public bool Focus(FocusState value)
		{
			if (value == FocusState.Unfocused)
			{
				// You can't remove focus from a control by calling this method with FocusState.Unfocused as the parameter. This value is not allowed and causes an exception. To remove focus from a control, set focus to a different control.
				throw new ArgumentException("Value does not fall within the expected range.", nameof(value));
			}

#if __WASM__
			return Visibility == Visibility.Visible && IsEnabled && RequestFocus(value);
#else
			return IsFocusable && RequestFocus(value);
#endif
		}

		internal void Unfocus()
		{
			FocusState = FocusState.Unfocused;
		}

		protected virtual void OnForegroundColorChanged(Brush oldValue, Brush newValue)
		{
			OnForegroundColorChangedPartial(oldValue, newValue);
		}

		partial void OnForegroundColorChangedPartial(Brush oldValue, Brush newValue);

		protected virtual void OnFontWeightChanged(FontWeight oldValue, FontWeight newValue)
		{
			OnFontWeightChangedPartial(oldValue, newValue);
		}

		partial void OnFontWeightChangedPartial(FontWeight oldValue, FontWeight newValue);

		protected virtual void OnFontFamilyChanged(FontFamily oldValue, FontFamily newValue)
		{
			OnFontFamilyChangedPartial(oldValue, newValue);
		}

		partial void OnFontFamilyChangedPartial(FontFamily oldValue, FontFamily newValue);

		protected virtual void OnFontSizeChanged(double oldValue, double newValue)
		{
			OnFontSizeChangedPartial(oldValue, newValue);
		}

		partial void OnFontSizeChangedPartial(double oldValue, double newValue);

		protected virtual void OnFontStyleChanged(FontStyle oldValue, FontStyle newValue)
		{
			OnFontStyleChangedPartial(oldValue, newValue);
		}

		partial void OnFontStyleChangedPartial(FontStyle oldValue, FontStyle newValue);

		protected virtual void OnPaddingChanged(Thickness oldValue, Thickness newValue)
		{
			OnPaddingChangedPartial(oldValue, newValue);
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue);

		protected virtual void OnBorderThicknessChanged(Thickness oldValue, Thickness newValue)
		{
			OnBorderThicknessChangedPartial(oldValue, newValue);
		}

		partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue);

		protected virtual void OnBorderBrushChanged(Brush oldValue, Brush newValue)
		{
			OnBorderBrushChangedPartial(oldValue, newValue);
		}

		partial void OnBorderBrushChangedPartial(Brush oldValue, Brush newValue);

		protected virtual void OnFocusStateChanged(FocusState oldValue, FocusState newValue)
		{
			if (newValue == FocusState.Unfocused && oldValue == newValue)
			{
				return;
			}

			OnFocusStateChangedPartial(oldValue, newValue);
#if XAMARIN || __WASM__
			FocusManager.OnFocusChanged(this, newValue);
#endif

			if (newValue == FocusState.Unfocused)
			{
				RaiseEvent(LostFocusEvent, new RoutedEventArgs(this));
			}
			else
			{
				RaiseEvent(GotFocusEvent, new RoutedEventArgs(this));
			}
		}

		partial void OnFocusStateChangedPartial(FocusState oldValue, FocusState newValue);

		protected virtual void OnPointerPressed(PointerRoutedEventArgs args) { }
		protected virtual void OnPointerReleased(PointerRoutedEventArgs args) { }
		protected virtual void OnPointerEntered(PointerRoutedEventArgs args) { }
		protected virtual void OnPointerExited(PointerRoutedEventArgs args) { }
		protected virtual void OnPointerMoved(PointerRoutedEventArgs args) { }
		protected virtual void OnPointerCanceled(PointerRoutedEventArgs args) { }
		protected virtual void OnPointerCaptureLost(PointerRoutedEventArgs args) { }
		protected virtual void OnManipulationStarting(ManipulationStartingRoutedEventArgs e) { }
		protected virtual void OnManipulationStarted(ManipulationStartedRoutedEventArgs e) { }
		protected virtual void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e) { }
		protected virtual void OnManipulationInertiaStarting(ManipulationInertiaStartingRoutedEventArgs e) { }
		protected virtual void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e) { }
		protected virtual void OnTapped(TappedRoutedEventArgs e) { }
		protected virtual void OnDoubleTapped(DoubleTappedRoutedEventArgs e) { }
		protected virtual void OnKeyDown(KeyRoutedEventArgs args) { }
		protected virtual void OnKeyUp(KeyRoutedEventArgs args) { }
		protected virtual void OnGotFocus(RoutedEventArgs e) { }
		protected virtual void OnLostFocus(RoutedEventArgs e) { }

		private static readonly PointerEventHandler OnPointerPressedHandler =
			(object sender, PointerRoutedEventArgs args) => ((Control)sender).OnPointerPressed(args);

		private static readonly PointerEventHandler OnPointerReleasedHandler =
			(object sender, PointerRoutedEventArgs args) => ((Control)sender).OnPointerReleased(args);

		private static readonly PointerEventHandler OnPointerEnteredHandler =
			(object sender, PointerRoutedEventArgs args) => ((Control)sender).OnPointerEntered(args);

		private static readonly PointerEventHandler OnPointerExitedHandler =
			(object sender, PointerRoutedEventArgs args) => ((Control)sender).OnPointerExited(args);

		private static readonly PointerEventHandler OnPointerMovedHandler =
			(object sender, PointerRoutedEventArgs args) => ((Control)sender).OnPointerMoved(args);

		private static readonly PointerEventHandler OnPointerCanceledHandler =
			(object sender, PointerRoutedEventArgs args) => ((Control)sender).OnPointerCanceled(args);

		private static readonly PointerEventHandler OnPointerCaptureLostHandler =
			(object sender, PointerRoutedEventArgs args) => ((Control)sender).OnPointerCaptureLost(args);

		private static readonly ManipulationStartingEventHandler OnManipulationStartingHandler =
			(object sender, ManipulationStartingRoutedEventArgs args) => ((Control)sender).OnManipulationStarting(args);

		private static readonly ManipulationStartedEventHandler OnManipulationStartedHandler =
			(object sender, ManipulationStartedRoutedEventArgs args) => ((Control)sender).OnManipulationStarted(args);

		private static readonly ManipulationDeltaEventHandler OnManipulationDeltaHandler =
			(object sender, ManipulationDeltaRoutedEventArgs args) => ((Control)sender).OnManipulationDelta(args);

		private static readonly ManipulationInertiaStartingEventHandler OnManipulationInertiaStartingHandler =
			(object sender, ManipulationInertiaStartingRoutedEventArgs args) => ((Control)sender).OnManipulationInertiaStarting(args);

		private static readonly ManipulationCompletedEventHandler OnManipulationCompletedHandler =
			(object sender, ManipulationCompletedRoutedEventArgs args) => ((Control)sender).OnManipulationCompleted(args);

		private static readonly TappedEventHandler OnTappedHandler =
			(object sender, TappedRoutedEventArgs args) => ((Control)sender).OnTapped(args);

		private static readonly DoubleTappedEventHandler OnDoubleTappedHandler =
			(object sender, DoubleTappedRoutedEventArgs args) => ((Control)sender).OnDoubleTapped(args);

		private static readonly KeyEventHandler OnKeyDownHandler =
			(object sender, KeyRoutedEventArgs args) => ((Control)sender).OnKeyDown(args);

		private static readonly KeyEventHandler OnKeyUpHandler =
			(object sender, KeyRoutedEventArgs args) => ((Control)sender).OnKeyUp(args);

		private static readonly RoutedEventHandler OnGotFocusHandler =
			(object sender, RoutedEventArgs args) => ((Control)sender).OnGotFocus(args);

		private static readonly RoutedEventHandler OnLostFocusHandler =
			(object sender, RoutedEventArgs args) => ((Control)sender).OnLostFocus(args);

		private static readonly Dictionary<Type, RoutedEventFlag> ImplementedRoutedEvents
			= new Dictionary<Type, RoutedEventFlag>();

		private static readonly Type[] _pointerArgsType = new[] { typeof(PointerRoutedEventArgs) };
		private static readonly Type[] _tappedArgsType = new[] { typeof(TappedRoutedEventArgs) };
		private static readonly Type[] _doubleTappedArgsType = new[] { typeof(DoubleTappedRoutedEventArgs) };
		private static readonly Type[] _keyArgsType = new[] { typeof(KeyRoutedEventArgs) };
		private static readonly Type[] _routedArgsType = new[] { typeof(RoutedEventArgs) };
		private static readonly Type[] _manipStartingArgsType = new[] { typeof(ManipulationStartingRoutedEventArgs) };
		private static readonly Type[] _manipStartedArgsType = new[] { typeof(ManipulationStartedRoutedEventArgs) };
		private static readonly Type[] _manipDeltaArgsType = new[] { typeof(ManipulationDeltaRoutedEventArgs) };
		private static readonly Type[] _manipInertiaArgsType = new[] { typeof(ManipulationInertiaStartingRoutedEventArgs) };
		private static readonly Type[] _manipCompletedArgsType = new[] { typeof(ManipulationCompletedRoutedEventArgs) };

		protected static RoutedEventFlag GetImplementedRoutedEvents(Type type)
		{
			// TODO: GetImplementedRoutedEvents() should be evaluated at compile-time
			// and the result placed in a partial file.

			if (ImplementedRoutedEvents.TryGetValue(type, out var result))
			{
				return result;
			}

			result = RoutedEventFlag.None;

			var baseClass = type.BaseType;
			if (baseClass == null || type == typeof(Control) || type == typeof(UIElement))
			{
				return result;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnPointerPressed), _pointerArgsType))
			{
				result |= RoutedEventFlag.PointerPressed;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnPointerReleased), _pointerArgsType))
			{
				result |= RoutedEventFlag.PointerReleased;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnPointerEntered), _pointerArgsType))
			{
				result |= RoutedEventFlag.PointerEntered;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnPointerExited), _pointerArgsType))
			{
				result |= RoutedEventFlag.PointerExited;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnPointerMoved), _pointerArgsType))
			{
				result |= RoutedEventFlag.PointerMoved;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnPointerCanceled), _pointerArgsType))
			{
				result |= RoutedEventFlag.PointerCanceled;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnPointerCaptureLost), _pointerArgsType))
			{
				result |= RoutedEventFlag.PointerCaptureLost;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnManipulationStarting), _manipStartingArgsType))
			{
				result |= RoutedEventFlag.ManipulationStarting;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnManipulationStarted), _manipStartedArgsType))
			{
				result |= RoutedEventFlag.ManipulationStarted;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnManipulationDelta), _manipDeltaArgsType))
			{
				result |= RoutedEventFlag.ManipulationDelta;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnManipulationInertiaStarting), _manipInertiaArgsType))
			{
				result |= RoutedEventFlag.ManipulationInertiaStarting;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnManipulationCompleted), _manipCompletedArgsType))
			{
				result |= RoutedEventFlag.ManipulationCompleted;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnTapped), _tappedArgsType))
			{
				result |= RoutedEventFlag.Tapped;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnDoubleTapped), _doubleTappedArgsType))
			{
				result |= RoutedEventFlag.DoubleTapped;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnKeyDown), _keyArgsType))
			{
				result |= RoutedEventFlag.KeyDown;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnKeyUp), _keyArgsType))
			{
				result |= RoutedEventFlag.KeyUp;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnLostFocus), _routedArgsType))
			{
				result |= RoutedEventFlag.LostFocus;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnGotFocus), _routedArgsType))
			{
				result |= RoutedEventFlag.GotFocus;
			}

			return ImplementedRoutedEvents[type] = result;
		}

		private static bool GetIsEventOverrideImplemented(Type type, string name, Type[] args)
		{
			var method = type
				.GetMethod(
					name,
					BindingFlags.NonPublic | BindingFlags.Instance,
					null,
					args,
					null);

			return method != null
				&& method.IsVirtual
				&& method.DeclaringType != typeof(Control);
		}
	}
}
