using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Windows.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;

#if XAMARIN_ANDROID
using Android.Views;
using Android.Graphics;
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using UIKit;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
using MonoTouch.UIKit;
#elif __MACOS__
using View = AppKit.NSView;
using Color = Windows.UI.Color;
#else
using Color = System.Drawing.Color;
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Child))]
	public partial class Border : FrameworkElement
	{

		/// <summary>        
		/// Support for the C# collection initializer style.
		/// Allows items to be added like this 
		/// new Border 
		/// {
		///    new Border()
		/// }
		/// </summary>
		/// <param name="view"></param>
		public
#if XAMARIN_IOS
			new
#endif
			void Add(View view)
		{
			Child = view;
		}

		protected override bool IsSimpleLayout => true;

		#region Child DependencyProperty

		public virtual View Child
		{
			get => (View)this.GetValue(ChildProperty);
			set => this.SetValue(ChildProperty, value);
		}

		public static readonly DependencyProperty ChildProperty =
			DependencyProperty.Register(
				"Child",
				typeof(View),//TODO Should be UIElement
				typeof(Border),
				new PropertyMetadata(
					null,
					(s, e) => ((Border)s)?.OnChildChanged((View)e.OldValue, (View)e.NewValue)
				)
			);

		protected void OnChildChanged(View oldValue, View newValue)
		{
			ReAttachChildTransitions(oldValue, newValue);

			OnChildChangedPartial(oldValue, newValue);
		}

		partial void OnChildChangedPartial(View previousValue, View newValue);

		#endregion

		#region CornerRadius
		public CornerRadius CornerRadius
		{
			get => (CornerRadius)this.GetValue(CornerRadiusProperty);
			set => this.SetValue(CornerRadiusProperty, value);
		}

		public static readonly DependencyProperty CornerRadiusProperty =
			DependencyProperty.Register(
				nameof(CornerRadius),
				typeof(CornerRadius),
				typeof(Border),
				new FrameworkPropertyMetadata(
					CornerRadius.None,
					FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
					(s, e) => ((Border)s)?.OnCornerRadiusChanged((CornerRadius)e.OldValue, (CornerRadius)e.NewValue)
				)
			);

		private void OnCornerRadiusChanged(CornerRadius oldValue, CornerRadius newValue)
		{
			OnCornerRadiusUpdatedPartial(oldValue, newValue);
		}

		partial void OnCornerRadiusUpdatedPartial(CornerRadius oldValue, CornerRadius newValue);
		#endregion

		#region ChildTransitions

		/// <summary>
		/// This is a Transition for a UIElement.
		/// </summary>
		public TransitionCollection ChildTransitions
		{
			get => (TransitionCollection)this.GetValue(ChildTransitionsProperty);
			set => this.SetValue(ChildTransitionsProperty, value);
		}

		// Using a DependencyProperty as the backing store for Transitions.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ChildTransitionsProperty =
			DependencyProperty.Register("ChildTransitions", typeof(TransitionCollection), typeof(Border), new PropertyMetadata(null));

		private void ReAttachChildTransitions(View originalChild, View child)
		{
			if (this.ChildTransitions == null)
			{
				return;
			}

			if (!(originalChild is IFrameworkElement oldTargetElement))
			{
				return;
			}

			foreach (var transition in this.ChildTransitions)
			{
				transition.DetachFromElement(oldTargetElement);
			}

			if (!(child is IFrameworkElement targetElement))
			{
				return;
			}

			foreach (var transition in this.ChildTransitions)
			{
				transition.AttachToElement(targetElement);
			}
		}


		#endregion

		#region Padding DependencyProperty

		public Thickness Padding
		{
			get => (Thickness)this.GetValue(PaddingProperty);
			set => this.SetValue(PaddingProperty, value);
		}

		public static readonly DependencyProperty PaddingProperty =
			DependencyProperty.Register(
				nameof(Padding),
				typeof(Thickness),
				typeof(Border),
				new FrameworkPropertyMetadata(
					Thickness.Empty,
					FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
					(s, e) => ((Border)s)?.OnPaddingChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
				)
			);
		protected virtual void OnPaddingChanged(Thickness oldValue, Thickness newValue)
		{
			OnPaddingChangedPartial(oldValue, newValue);
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue);



		#endregion

		#region BorderThickness DependencyProperty

		public Thickness BorderThickness
		{
			get => (Thickness)this.GetValue(BorderThicknessProperty);
			set => this.SetValue(BorderThicknessProperty, value);
		}

		// Using a DependencyProperty as the backing store for BorderThickness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BorderThicknessProperty =
			DependencyProperty.Register(
				nameof(BorderThickness),
				typeof(Thickness),
				typeof(Border),
				new FrameworkPropertyMetadata(
					Thickness.Empty,
					FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
					(s, e) => ((Border)s)?.OnBorderThicknessChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
				)
			);

		protected virtual void OnBorderThicknessChanged(Thickness oldValue, Thickness newValue)
		{
			OnBorderThicknessChangedPartial(oldValue, newValue);
		}

		partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue);

		#endregion

		#region BorderBrush Dependency Property


		private SerialDisposable _borderBrushColorChanged = new SerialDisposable();
		private SerialDisposable _borderBrushOpacityChanged = new SerialDisposable();

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
				typeof(Border),
				new FrameworkPropertyMetadata(
					SolidColorBrushHelper.Transparent,
					FrameworkPropertyMetadataOptions.ValueInheritsDataContext,
					propertyChangedCallback: (s, e) => ((Border)s).OnBorderBrushChanged((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);

		protected virtual void OnBorderBrushChanged(Brush oldValue, Brush newValue)
		{
			var colorBrush = newValue as SolidColorBrush;

			if (colorBrush != null)
			{
				_borderBrushColorChanged.Disposable = colorBrush.RegisterDisposablePropertyChangedCallback(
					SolidColorBrush.ColorProperty,
					(s, colorArg) => OnBorderBrushChangedPartial()
				);
				_borderBrushOpacityChanged.Disposable = colorBrush.RegisterDisposablePropertyChangedCallback(
					SolidColorBrush.OpacityProperty,
					(s, _) => OnBorderBrushChangedPartial()
				);
			}
			else
			{
				_borderBrushColorChanged.Disposable = null;
				_borderBrushOpacityChanged.Disposable = null;
			}

			OnBorderBrushChangedPartial();
		}

		partial void OnBorderBrushChangedPartial();

		#endregion
	}
}
