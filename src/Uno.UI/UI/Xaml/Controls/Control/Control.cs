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
#elif __WASM__ || NET46
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
			OnIsFocusableChanged();
		}

		/// <summary>
		/// This property is not used in Uno.UI, and is always set to the current top-level type.
		/// </summary>
		protected object DefaultStyleKey { get; set; }

		protected override bool IsSimpleLayout => true;

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

		protected override void OnLoaded()
		{
			SetUpdateControlTemplate();

			base.OnLoaded();

			PointerPressed += OnPointerPressed;
			PointerReleased += OnPointerReleased;
			PointerMoved += OnPointerMoved;
			PointerEntered += OnPointerEntered;
			PointerExited += OnPointerExited;
			PointerCanceled += OnPointerCanceled;
			PointerCaptureLost += OnPointerCaptureLost;
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();
			
			PointerPressed -= OnPointerPressed;
			PointerReleased -= OnPointerReleased;
			PointerMoved -= OnPointerMoved;
			PointerEntered -= OnPointerEntered;
			PointerExited -= OnPointerExited;
			PointerCanceled -= OnPointerCanceled;
			PointerCaptureLost -= OnPointerCaptureLost;
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
		/// Finds a realised element in the control template
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
			Visibility == Visibility.Visible &&
			IsEnabled &&
			IsTabStop;

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
			OnFocusStateChangedPartial(oldValue, newValue);
#if XAMARIN || __WASM__
			FocusManager.OnFocusChanged(this, newValue);
#endif

			var eventArgs = new RoutedEventArgs
			{
				OriginalSource = this,
			};

			if (newValue == FocusState.Unfocused)
			{
				OnLostFocus(eventArgs);
				RaiseEvent(LostFocusEvent, eventArgs);
			}
			else
			{
				OnGotFocus(eventArgs);
				RaiseEvent(GotFocusEvent, eventArgs);
			}
		}

		partial void OnFocusStateChangedPartial(FocusState oldValue, FocusState newValue);

		protected virtual void OnLostFocus(RoutedEventArgs e) { }

		protected virtual void OnGotFocus(RoutedEventArgs e) { }

		protected virtual void OnPointerPressed(PointerRoutedEventArgs args) { }

		protected virtual void OnPointerReleased(PointerRoutedEventArgs args) { }

		protected virtual void OnPointerEntered(PointerRoutedEventArgs args) { }

		protected virtual void OnPointerExited(PointerRoutedEventArgs args) { }

		protected virtual void OnPointerMoved(PointerRoutedEventArgs args) { }

		protected virtual void OnPointerCanceled(PointerRoutedEventArgs args) { }

		protected virtual void OnPointerCaptureLost(PointerRoutedEventArgs args) { }

		private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			OnPointerPressed(args);
		}

		private void OnPointerReleased(object sender, PointerRoutedEventArgs args)
		{
			OnPointerReleased(args);
		}

		private void OnPointerEntered(object sender, PointerRoutedEventArgs args)
		{
			OnPointerEntered(args);
		}

		private void OnPointerExited(object sender, PointerRoutedEventArgs args)
		{
			OnPointerExited(args);
		}

		private void OnPointerMoved(object sender, PointerRoutedEventArgs args)
		{
			OnPointerMoved(args);
		}

		private void OnPointerCanceled(object sender, PointerRoutedEventArgs args)
		{
			OnPointerCanceled(args);
		}

		private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs args)
		{
			OnPointerCaptureLost(args);
		}
	}
}
