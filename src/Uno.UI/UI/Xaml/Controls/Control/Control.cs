using System;
using Uno.UI;
using System.Linq;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Windows.UI.Xaml.Markup;
using System.ComponentModel;
using Uno.UI.Xaml;
using Windows.Foundation;
using Uno;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
#if __ANDROID__
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif __IOS__
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
#elif UNO_REFERENCE_API || IS_UNIT_TESTS
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Control : FrameworkElement
	{
		private bool _suspendStateChanges;
		private View _templatedRoot;
		private bool _updateTemplate;
		private bool _suppressIsEnabled;

		private void InitializeControl()
		{
			SetDefaultForeground(ForegroundProperty);
			SubscribeToOverridenRoutedEvents();
			OnIsFocusableChanged();

			DefaultStyleKey = typeof(Control);
		}

		// TODO: Should use DefaultStyleKeyProperty DP
		protected object DefaultStyleKey { get; set; }

		private protected override bool IsTabStopDefaultValue => true;

		protected override bool IsSimpleLayout => true;

		internal override bool IsEnabledOverride() => IsEnabled && base.IsEnabledOverride();

		internal override void UpdateThemeBindings(Data.ResourceUpdateReason updateReason)
		{
			base.UpdateThemeBindings(updateReason);

			//override the default value from dependency property based on application theme
			SetDefaultForeground(ForegroundProperty);
		}

		private protected override Type GetDefaultStyleKey() => DefaultStyleKey as Type;

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// this is defined in the FrameworkElement mixin, and must not be used in Control.
			// When setting the background color in a Control, the property is simply used as a placeholder
			// for children controls, applied by inheritance.
		}

		internal virtual void UpdateVisualState(bool useTransitions = true)
		{
			if (!_suspendStateChanges)
			{
				ChangeVisualState(useTransitions);
			}
		}

		private protected virtual void ChangeVisualState(bool useTransitions)
		{
		}

		/// <summary>
		/// Will be set to Template when it is applied
		/// </summary>
		private ControlTemplate _controlTemplateUsedLastUpdate;

		partial void UnregisterSubView();
		partial void RegisterSubView(View child);

		#region IsEnabled DependencyProperty

		// Note: we keep the event args as a private field for perf consideration: This avoids creating a new instance each time.
		//		 As it's used only internally it's safe to do so.
		[ThreadStatic]
		private static IsEnabledChangedEventArgs _isEnabledChangedEventArgs;

		public event DependencyPropertyChangedEventHandler IsEnabledChanged;

		[GeneratedDependencyProperty(DefaultValue = true, ChangedCallback = true, CoerceCallback = true, Options = FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.KeepCoercedWhenEquals)]
		public static DependencyProperty IsEnabledProperty { get; } = CreateIsEnabledProperty();

		public bool IsEnabled
		{
			get => GetIsEnabledValue();
			set => SetIsEnabledValue(value);
		}

		private void OnIsEnabledChanged(DependencyPropertyChangedEventArgs args)
		{
#if UNO_HAS_MANAGED_POINTERS || __WASM__
			UpdateHitTest();
#endif

			_isEnabledChangedEventArgs ??= new IsEnabledChangedEventArgs();
			_isEnabledChangedEventArgs.SourceEvent = args;

			OnIsEnabledChanged(_isEnabledChangedEventArgs);

#if __ANDROID__
			var newValue = (bool)args.NewValue;
			base.SetNativeIsEnabled(newValue);
			this.Enabled = newValue;
#elif __IOS__
			UserInteractionEnabled = (bool)args.NewValue;
#elif __MACOS__
			// UserInteractionEnabled = (bool)args.NewValue; // UNO-TODO: Set MacOS native equivalent
#endif

			IsEnabledChanged?.Invoke(this, args);

			// TODO: move focus elsewhere if control.FocusState != FocusState.Unfocused
#if __WASM__
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMProperties();
			}
#endif
		}
		#endregion

		internal bool IsEnabledSuppressed => _suppressIsEnabled;

		/// <summary>
		/// Provides the ability to disable <see cref="IsEnabled"/> value changes, e.g. in the context of ICommand CanExecute.
		/// </summary>
		/// <param name="suppress">If true, <see cref="IsEnabled"/> will always be false</param>
		private protected void SuppressIsEnabled(bool suppress)
		{
			if (_suppressIsEnabled != suppress)
			{
				_suppressIsEnabled = suppress;
				this.CoerceValue(IsEnabledProperty);
			}
		}

		private protected virtual object CoerceIsEnabled(object baseValue, DependencyPropertyValuePrecedences precedence)
		{
			if (_suppressIsEnabled)
			{
				return false;
			}

			// The baseValue hasn't been set inside PropertyDetails yet, so we need to make sure we're not
			// reading soon-to-be-outdated values

			var parentValue = precedence == DependencyPropertyValuePrecedences.Inheritance ?
				baseValue :
				this.GetValue(IsEnabledProperty, DependencyPropertyValuePrecedences.Inheritance);

			// If the parent is disabled, this control must be disabled as well
			if (parentValue is false)
			{
				return false;
			}

			// otherwise use the more local value
			var (localValue, localPrecedence) = this.GetValueUnderPrecedence(IsEnabledProperty, DependencyPropertyValuePrecedences.Coercion);

			if (localPrecedence >= precedence) // > means weaker precedence
			{
				// The baseValue hasn't been set inside PropertyDetails yet, so we need to make sure we're not
				// using the old weaker value when a new stronger value is being set
				localValue = baseValue;
			}

			return localValue;
		}


		#region Template DependencyProperty

		public ControlTemplate Template
		{
			get { return (ControlTemplate)GetValue(TemplateProperty); }
			set { SetValue(TemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Template.  This enables animation, styling, binding, etc...
		public static DependencyProperty TemplateProperty { get; } =
			DependencyProperty.Register("Template", typeof(ControlTemplate), typeof(Control), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, (s, e) => ((Control)s)?.OnTemplateChanged(e)));

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
#if !UNO_HAS_ENHANCED_LIFECYCLE
				!FeatureConfiguration.Control.UseLegacyLazyApplyTemplate ||
#endif
				forceUpdate ||
				this.HasParent() ||
				CanCreateTemplateWithoutParent
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
				if (_templatedRoot == value)
				{
					return;
				}

				CleanupView(_templatedRoot);

				UnregisterSubView();

				_templatedRoot = value;

				if (value != null)
				{
					if (_templatedRoot is IDependencyObjectStoreProvider provider)
					{
						provider.Store.SetValue(provider.Store.TemplatedParentProperty, this, DependencyPropertyValuePrecedences.Local);
					}

					RegisterSubView(value);

					if (_templatedRoot != null)
					{
						RegisterContentTemplateRoot();

						if (
#if __CROSSRUNTIME__
							!IsLoading &&
#endif
							!IsLoaded && FeatureConfiguration.Control.UseDeferredOnApplyTemplate)
						{
							// It's too soon the call the ".OnApplyTemplate" method: it should be invoked after the "Loading" event.

							// Note: we however still allow if already 'IsLoading':
							//
							// If this child is added to its parent while this parent is 'IsLoading' itself (eg. loading its template),
							// the parent will invoke the Loading on this child element (and the PostLoading which will "dequeue" the _applyTemplateShouldBeInvoked),
							// which will set the 'IsLoading' flag.
							//
							// The parent will then apply its own style, which might set/change the template of this element (if data-bound or set using VisualState),
							// which would end here and set this _applyTemplateShouldBeInvoked flag (if IsLoaded were not allowed!).
							//
							// The parent will then invoke the Loading on all its children, but as this child has already been flagged as 'IsLoading',
							// it will be ignored and the 'PostLoading' won't be invokes a second time, driving the control to never "dequeue" the _applyTemplateShouldBeInvoked.
							_applyTemplateShouldBeInvoked = true;
						}
						else
						{
							_applyTemplateShouldBeInvoked = false;
							OnApplyTemplate();
						}
					}
				}
			}
		}

		private bool _applyTemplateShouldBeInvoked;

#if __ANDROID__ || __IOS__ || __MACOS__ || IS_UNIT_TESTS
		private protected override void OnPostLoading()
		{
			base.OnPostLoading();

			TryCallOnApplyTemplate();

			// Update bindings to ensure resources defined
			// in visual parents get applied.
			this.UpdateResourceBindings();
		}
#endif

		internal void TryCallOnApplyTemplate()
		{
			if (_applyTemplateShouldBeInvoked)
			{
				_applyTemplateShouldBeInvoked = false;
				OnApplyTemplate();
			}
		}

		private void SubscribeToOverridenRoutedEvents()
		{
			// Overridden Events are registered from constructor to ensure they are
			// registered first in event handlers.
			// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.control.onpointerpressed#remarks

			var implementedEvents = GetImplementedRoutedEventsForType(GetType());

			if (HasFlag(implementedEvents, RoutedEventFlag.PointerPressed))
			{
				PointerPressed += OnPointerPressedHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.PointerReleased))
			{
				PointerReleased += OnPointerReleasedHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.PointerMoved))
			{
				PointerMoved += OnPointerMovedHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.PointerEntered))
			{
				PointerEntered += OnPointerEnteredHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.PointerExited))
			{
				PointerExited += OnPointerExitedHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.PointerCanceled))
			{
				PointerCanceled += OnPointerCanceledHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.PointerCaptureLost))
			{
				PointerCaptureLost += OnPointerCaptureLostHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.PointerWheelChanged))
			{
				PointerWheelChanged += OnPointerWheelChangedHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.ManipulationStarting))
			{
				ManipulationStarting += OnManipulationStartingHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.ManipulationStarted))
			{
				ManipulationStarted += OnManipulationStartedHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.ManipulationDelta))
			{
				ManipulationDelta += OnManipulationDeltaHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.ManipulationInertiaStarting))
			{
				ManipulationInertiaStarting += OnManipulationInertiaStartingHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.ManipulationCompleted))
			{
				ManipulationCompleted += OnManipulationCompletedHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.Tapped))
			{
				Tapped += OnTappedHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.DoubleTapped))
			{
				DoubleTapped += OnDoubleTappedHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.RightTapped))
			{
				RightTapped += OnRightTappedHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.DragEnter))
			{
				DragEnter += OnDragEnterHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.DragOver))
			{
				DragOver += OnDragOverHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.DragLeave))
			{
				DragLeave += OnDragLeaveHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.Drop))
			{
				Drop += OnDropHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.Holding))
			{
				Holding += OnHoldingHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.KeyDown))
			{
				KeyDown += OnKeyDownHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.KeyUp))
			{
				KeyUp += OnKeyUpHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.GotFocus))
			{
				GotFocus += OnGotFocusHandler;
			}

			if (HasFlag(implementedEvents, RoutedEventFlag.LostFocus))
			{
				LostFocus += OnLostFocusHandler;
			}

			bool HasFlag(RoutedEventFlag implementedEvents, RoutedEventFlag flag) => (implementedEvents & flag) != 0;
		}

		private protected override void OnLoaded()
		{
			SetUpdateControlTemplate();

			base.OnLoaded();

		}

		protected override Size MeasureOverride(Size availableSize)
			=> MeasureFirstChild(availableSize);

		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeFirstChild(finalSize);

		/// <summary>
		/// Loads the relevant control template so that its parts can be referenced.
		/// </summary>
		/// <returns>A value that indicates whether the visual tree was rebuilt by this call. True if the tree was rebuilt; false if the previous visual tree was retained.</returns>
		public bool ApplyTemplate()
		{
			var currentTemplateRoot = _templatedRoot;
			SetUpdateControlTemplate(forceUpdate: true);

			// When .ApplyTemplate is called manually, we should not defer the call to OnApplyTemplate
			TryCallOnApplyTemplate();

			return currentTemplateRoot != _templatedRoot;
		}

		/// <summary>
		/// Applies default Style and implicit/explicit Style if not applied already, and materializes template.
		/// </summary>
		internal void EnsureTemplate()
		{
			ApplyStyles();
			ApplyTemplate();
		}

		/// <summary>
		/// Finds a realized element in the control template.
		/// </summary>
		/// <param name="childName">The name of the template part.</param>
		/// <returns>The first template part of the specified name; otherwise, null.</returns>
		public DependencyObject GetTemplateChild(string childName)
		{
			return FindNameInScope(TemplatedRoot as IFrameworkElement, childName) as DependencyObject
				?? FindName(childName) as DependencyObject;
		}

		/// <summary>
		/// Finds a realized element in the control template of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the template part.</typeparam>
		/// <param name="childName">The name of the template part.</param>
		/// <returns>The first template part of the specified name; otherwise, null.</returns>
		internal T GetTemplateChild<T>(string childName) where T : class, DependencyObject
		{
			return FindNameInScope(TemplatedRoot as IFrameworkElement, childName) as T ?? FindName(childName) as T;
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
		protected virtual bool CanCreateTemplateWithoutParent { get; }

		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			base.OnVisibilityChanged(oldValue, newValue);

			if (oldValue == Visibility.Collapsed && newValue == Visibility.Visible)
			{
				SetUpdateControlTemplate();
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

		partial void RegisterContentTemplateRoot();

		#region Foreground Dependency Property

		public
#if __ANDROID__
		new
#endif
		Brush Foreground
		{
			get { return (Brush)this.GetValue(ForegroundProperty); }
			set { this.SetValue(ForegroundProperty, value); }
		}

		public static DependencyProperty ForegroundProperty { get; } =
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

		public static DependencyProperty FontWeightProperty { get; } =
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

		public static DependencyProperty FontSizeProperty { get; } =
			DependencyProperty.Register(
				"FontSize",
				typeof(double),
				typeof(Control),
				new FrameworkPropertyMetadata(
					14.0,
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

		public static DependencyProperty FontFamilyProperty { get; } =
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

		public static DependencyProperty FontStyleProperty { get; } =
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
		public static DependencyProperty PaddingProperty { get; } =
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
		public static DependencyProperty BorderThicknessProperty { get; } =
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

#if __ANDROID__
		//This field is never accessed. It just exists to create a reference, because the DP causes issues with ImageBrush of the backing bitmap being prematurely garbage-collected. (Bug with ConditionalWeakTable? https://bugzilla.xamarin.com/show_bug.cgi?id=21620)
		private Brush _borderBrushStrongReference;
#endif

		public Brush BorderBrush
		{
			get { return (Brush)this.GetValue(BorderBrushProperty); }
			set
			{
				this.SetValue(BorderBrushProperty, value);

#if __ANDROID__
				_borderBrushStrongReference = value;
#endif
			}
		}

		// Using a DependencyProperty as the backing store for BorderBrush.  This enables animation, styling, binding, etc...
		public static DependencyProperty BorderBrushProperty { get; } =
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

		public CornerRadius CornerRadius
		{
			get => GetCornerRadiusValue();
			set => SetCornerRadiusValue(value);
		}

		public static CornerRadius GetCornerRadiusDefaultValue() => default(CornerRadius);

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnCornerRadiousChanged))]
		public static DependencyProperty CornerRadiusProperty { get; } = CreateCornerRadiusProperty();

		private protected virtual void OnCornerRadiousChanged(DependencyPropertyChangedEventArgs args)
		{
		}


		#endregion

#if HAS_UNO_WINUI
		private protected override void OnIsTabStopChanged(bool oldValue, bool newValue)
		{
			OnIsFocusableChanged();
		}
#endif

		#region TabNavigation DependencyProperty

		public KeyboardNavigationMode TabNavigation
		{
			get => TabFocusNavigation;
			set => TabFocusNavigation = value;
		}

		public static DependencyProperty TabNavigationProperty => UIElement.TabFocusNavigationProperty;

		#endregion

		public static bool GetIsTemplateFocusTarget(FrameworkElement element) =>
			GetIsTemplateFocusTargetValue(element);

		public static void SetIsTemplateFocusTarget(FrameworkElement element, bool value) =>
			SetIsTemplateFocusTargetValue(element, value);

		[GeneratedDependencyProperty(DefaultValue = false, AttachedBackingFieldOwner = typeof(Control), Attached = true)]
		public static DependencyProperty IsTemplateFocusTargetProperty { get; } = CreateIsTemplateFocusTargetProperty();

		/// <summary>
		/// Get or sets a value that indicates whether focus is constrained
		/// within the control boundaries (for game pad/remote interaction).
		/// </summary>
		public bool IsFocusEngaged
		{
			get => GetIsFocusEngagedValue();
			set => SetIsFocusEngagedValue(value);
		}

		/// <summary>
		/// Identifies the IsFocusEngaged dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = false, ChangedCallback = true)]
		public static DependencyProperty IsFocusEngagedProperty { get; } = CreateIsFocusEngagedProperty();

		private void OnIsFocusEngagedChanged(bool oldValue, bool newValue) => SetFocusEngagement();

		/// <summary>
		/// Get or sets a value that indicates whether focus can be constrained within
		/// the control boundaries (for game pad/remote interaction).
		/// </summary>
		public bool IsFocusEngagementEnabled
		{
			get => GetIsFocusEngagementEnabledValue();
			set => SetIsFocusEngagementEnabledValue(value);
		}

		/// <summary>
		/// Identifies the IsFocusEngagementEnabled dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = false)]
		public static DependencyProperty IsFocusEngagementEnabledProperty { get; } = CreateIsFocusEngagementEnabledProperty();

		private void OnIsFocusEngagementEnabledChanged(bool oldValue, bool newValue) => RemoveFocusEngagement();

		internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);
			OnDataContextChanged();
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected virtual void OnDataContextChanged()
		{
		}

		private protected virtual void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			OnIsFocusableChanged();

			// Part of logic from MUX Control.cpp Enabled method.
			var focusManager = VisualTree.GetFocusManagerForElement(this);
			if (focusManager == null)
			{
				return;
			}

			// Set focus if this control is the first focusable control in case of
			// no having focus yet
			if (IsFocusable)
			{
				if (focusManager.FocusedElement == null)
				{
					// No focused control here, so try to check the control is
					// the first focusable control
					var focusable = focusManager.GetFirstFocusableElement();
					if (this == focusable)
					{
						// If we are trying to set focus in a changing focus event handler, we will end up leaving focus on the disabled control.
						// As a result, we fail fast here. This is being tracked by Bug 9840123
						Focus(FocusState.Programmatic, animateIfBringIntoView: false);
					}
				}
			}

			// TODO Uno specific: Adding check for IsEnabledSuppressed here
			// as we need to make sure that when a Command is executing, the control
			// should not lose focus
			bool shouldReevaluateFocus =
				!(IsEnabled || IsEnabledSuppressed) && //!pControl->ParserOwnsParent()
				!AllowFocusWhenDisabled &&
				// We just disabled this control, find if this control
				//or one of its children had focus.
				FocusProperties.HasFocusedElement(this);

			if (shouldReevaluateFocus)
			{
				// Set the focus on the next focusable control.
				focusManager.SetFocusOnNextFocusableElement(focusManager.GetRealFocusStateForFocusedElement(), true);
			}
		}

		partial void OnIsFocusableChanged();

		public event TypedEventHandler<Control, FocusDisengagedEventArgs> FocusDisengaged;

		public event TypedEventHandler<Control, FocusEngagedEventArgs> FocusEngaged;

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

		protected virtual void OnPointerPressed(PointerRoutedEventArgs e) { }
		protected virtual void OnPointerReleased(PointerRoutedEventArgs e) { }
		protected virtual void OnPointerEntered(PointerRoutedEventArgs e) { }
		protected virtual void OnPointerExited(PointerRoutedEventArgs e) { }
		protected virtual void OnPointerMoved(PointerRoutedEventArgs e) { }
		protected virtual void OnPointerCanceled(PointerRoutedEventArgs e) { }
		protected virtual void OnPointerCaptureLost(PointerRoutedEventArgs e) { }
#if !__WASM__
		[global::Uno.NotImplemented]
#endif
		protected virtual void OnPointerWheelChanged(PointerRoutedEventArgs e) { }
		protected virtual void OnManipulationStarting(ManipulationStartingRoutedEventArgs e) { }
		protected virtual void OnManipulationStarted(ManipulationStartedRoutedEventArgs e) { }
		protected virtual void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e) { }
		protected virtual void OnManipulationInertiaStarting(ManipulationInertiaStartingRoutedEventArgs e) { }
		protected virtual void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e) { }
		protected virtual void OnTapped(TappedRoutedEventArgs e) { }
		protected virtual void OnDoubleTapped(DoubleTappedRoutedEventArgs e) { }
		protected virtual void OnRightTapped(RightTappedRoutedEventArgs e) { }
		[NotImplemented] // For WinUI code compatibility, not implemented yet
		private protected virtual void OnRightTappedUnhandled(RightTappedRoutedEventArgs e) { }
		protected virtual void OnHolding(HoldingRoutedEventArgs e) { }
		protected virtual void OnDragEnter(global::Windows.UI.Xaml.DragEventArgs e) { }
		protected virtual void OnDragOver(global::Windows.UI.Xaml.DragEventArgs e) { }
		protected virtual void OnDragLeave(global::Windows.UI.Xaml.DragEventArgs e) { }
		protected virtual void OnDrop(global::Windows.UI.Xaml.DragEventArgs e) { }
#if __WASM__ || __SKIA__
		protected virtual void OnPreviewKeyDown(KeyRoutedEventArgs e) { }
		protected virtual void OnPreviewKeyUp(KeyRoutedEventArgs e) { }
#endif
		protected virtual void OnKeyDown(KeyRoutedEventArgs e) { }
		protected virtual void OnKeyUp(KeyRoutedEventArgs e) { }
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

		private static readonly PointerEventHandler OnPointerWheelChangedHandler =
			(object sender, PointerRoutedEventArgs args) => ((Control)sender).OnPointerWheelChanged(args);

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

		private static readonly RightTappedEventHandler OnRightTappedHandler =
			(object sender, RightTappedRoutedEventArgs args) => ((Control)sender).OnRightTapped(args);

		private static readonly HoldingEventHandler OnHoldingHandler =
			(object sender, HoldingRoutedEventArgs args) => ((Control)sender).OnHolding(args);

		private static readonly DragEventHandler OnDragEnterHandler =
			(object sender, global::Windows.UI.Xaml.DragEventArgs args) => ((Control)sender).OnDragEnter(args);

		private static readonly DragEventHandler OnDragOverHandler =
			(object sender, global::Windows.UI.Xaml.DragEventArgs args) => ((Control)sender).OnDragOver(args);

		private static readonly DragEventHandler OnDragLeaveHandler =
			(object sender, global::Windows.UI.Xaml.DragEventArgs args) => ((Control)sender).OnDragLeave(args);

		private static readonly DragEventHandler OnDropHandler =
			(object sender, global::Windows.UI.Xaml.DragEventArgs args) => ((Control)sender).OnDrop(args);
#if __WASM__ || __SKIA__
		private static readonly KeyEventHandler OnPreviewKeyDownHandler =
			(object sender, KeyRoutedEventArgs args) => ((Control)sender).OnPreviewKeyDown(args);

		private static readonly KeyEventHandler OnPreviewKeyUpHandler =
			(object sender, KeyRoutedEventArgs args) => ((Control)sender).OnPreviewKeyUp(args);
#endif

		private static readonly KeyEventHandler OnKeyDownHandler =
			(object sender, KeyRoutedEventArgs args) =>
			{
				var senderAsControl = (Control)sender;
				// ToolTipService.CloseToolTipInternal(args); TODO:MZ:KA
				ProcessAcceleratorsIfApplicable(args, senderAsControl);
				if (!args.Handled)
				{
					senderAsControl.OnKeyDown(args);
				}
			};

		private static readonly KeyEventHandler OnKeyUpHandler =
			(object sender, KeyRoutedEventArgs args) => ((Control)sender).OnKeyUp(args);

		private static readonly RoutedEventHandler OnGotFocusHandler =
			(object sender, RoutedEventArgs args) => ((Control)sender).OnGotFocus(args);

		private static readonly RoutedEventHandler OnLostFocusHandler =
			(object sender, RoutedEventArgs args) => ((Control)sender).OnLostFocus(args);

		private static readonly Type[] _pointerArgsType = new[] { typeof(PointerRoutedEventArgs) };
		private static readonly Type[] _tappedArgsType = new[] { typeof(TappedRoutedEventArgs) };
		private static readonly Type[] _doubleTappedArgsType = new[] { typeof(DoubleTappedRoutedEventArgs) };
		private static readonly Type[] _rightTappedArgsType = new[] { typeof(RightTappedRoutedEventArgs) };
		private static readonly Type[] _holdingArgsType = new[] { typeof(HoldingRoutedEventArgs) };
		private static readonly Type[] _dragArgsType = new[] { typeof(global::Windows.UI.Xaml.DragEventArgs) };
		private static readonly Type[] _keyArgsType = new[] { typeof(KeyRoutedEventArgs) };
		private static readonly Type[] _routedArgsType = new[] { typeof(RoutedEventArgs) };
		private static readonly Type[] _manipStartingArgsType = new[] { typeof(ManipulationStartingRoutedEventArgs) };
		private static readonly Type[] _manipStartedArgsType = new[] { typeof(ManipulationStartedRoutedEventArgs) };
		private static readonly Type[] _manipDeltaArgsType = new[] { typeof(ManipulationDeltaRoutedEventArgs) };
		private static readonly Type[] _manipInertiaArgsType = new[] { typeof(ManipulationInertiaStartingRoutedEventArgs) };
		private static readonly Type[] _manipCompletedArgsType = new[] { typeof(ManipulationCompletedRoutedEventArgs) };

		internal static RoutedEventFlag EvaluateImplementedControlRoutedEvents(Type type)
		{
			var result = RoutedEventFlag.None;

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

			if (GetIsEventOverrideImplemented(type, nameof(OnPointerWheelChanged), _pointerArgsType))
			{
				result |= RoutedEventFlag.PointerWheelChanged;
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

			if (GetIsEventOverrideImplemented(type, nameof(OnRightTapped), _rightTappedArgsType))
			{
				result |= RoutedEventFlag.RightTapped;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnHolding), _holdingArgsType))
			{
				result |= RoutedEventFlag.Holding;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnDragEnter), _dragArgsType))
			{
				result |= RoutedEventFlag.DragEnter;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnDragOver), _dragArgsType))
			{
				result |= RoutedEventFlag.DragOver;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnDragLeave), _dragArgsType))
			{
				result |= RoutedEventFlag.DragLeave;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnDrop), _dragArgsType))
			{
				result |= RoutedEventFlag.Drop;
			}
#if __WASM__ || __SKIA__
			if (GetIsEventOverrideImplemented(type, nameof(OnPreviewKeyDown), _keyArgsType))
			{
				result |= RoutedEventFlag.PreviewKeyDown;
			}

			if (GetIsEventOverrideImplemented(type, nameof(OnPreviewKeyUp), _keyArgsType))
			{
				result |= RoutedEventFlag.PreviewKeyUp;
			}
#endif
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

			return result;
		}

		/// <summary>
		/// Duplicates the SetDefaultStyleKey() helper method from WinUI code.
		/// </summary>
		/// <remarks>
		/// Note: Although this is usually called as 'SetDefaultStyleKey(this)' (per WinUI C++ code), we actually only use the compile-time
		///  TDerived type and ignore the runtime derivedControl parameter, preserving the expected behaviour that DefaultStyleKey is 'fixed'
		/// under inheritance unless explicitly changed by an inheriting type.
		/// </remarks>
		private protected void SetDefaultStyleKey<TDerived>(TDerived derivedControl) where TDerived : Control
			=> DefaultStyleKey = typeof(TDerived);

		private protected bool GoToState(bool useTransitions, string stateName) => VisualStateManager.GoToState(this, stateName, useTransitions);

		// This is a method to support code from WinUI
		private protected void GoToState(bool useTransitions, string stateName, out bool b)
		{
			b = VisualStateManager.GoToState(this, stateName, useTransitions);
		}

#if DEBUG
#if !__IOS__
		public VisualStateGroup[] VisualStateGroups => VisualStateManager.GetVisualStateGroups(GetTemplateRoot()).ToArray();
#endif

		public string[] VisualStateGroupNames => VisualStateGroups.Select(vsg => vsg.Name).ToArray();

		public string[] CurrentVisualStates => VisualStateGroups.Select(vsg => vsg.CurrentState?.Name).ToArray();
#endif

		// This is a method to support code from WinUI
		internal void ConditionallyGetTemplatePartAndUpdateVisibility<T>(
			string strName,
			bool visible,
			ref T element) where T : UIElement
		{
			if (element == null && (visible /*|| !DXamlCore::GetCurrent()->GetHandle()->GetDeferredElementIfExists(strName, GetHandle(), Jupiter::NameScoping::NameScopeType::TemplateNameScope))*/))
			{
				// If element should be visible or is not deferred, then fetch it.
				element = GetTemplateChild(strName) as T;
			}

			// If element was found then set its Visibility - this is behavior consistent with pre-Threshold releases.
			if (element != null)
			{
				var spElementAsUIE = element as UIElement;

				if (spElementAsUIE != null)
				{
					spElementAsUIE.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		}

		internal override bool CanHaveChildren() => true;
	}
}
