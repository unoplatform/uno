using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Animation;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;

#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
#endif

namespace Windows.UI.Xaml.Controls
{
	[Markup.ContentProperty(Name = "Children")]
	public partial class Panel : FrameworkElement, ICustomClippingElement
	{
#if NET461 || __WASM__
		private new UIElementCollection _children;
#else
		private UIElementCollection _children;
#endif

		private PanelTransitionHelper _transitionHelper;

		partial void Initialize()
		{
			_children = new UIElementCollection(this);

			IFrameworkElementHelper.Initialize(this);
		}

		private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
			=> OnChildrenChanged();

		protected override void OnLoaded()
		{
			base.OnLoaded();

			_children.CollectionChanged += OnChildrenCollectionChanged;

			OnLoadedPartial();
		}

		partial void OnLoadedPartial();

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_children.CollectionChanged -= OnChildrenCollectionChanged;

			OnUnloadedPartial();
		}

		partial void OnUnloadedPartial();

		protected virtual void OnChildAdded(IFrameworkElement element)
		{
			UpdateTransitions(element);
		}

		private void UpdateTransitions(IFrameworkElement element)
		{
			if (_transitionHelper == null)
			{
				return;
			}

			_transitionHelper.AddElement(element);
		}

		public UIElementCollection Children => _children;

#region ChildrenTransitions Dependency Property

		public TransitionCollection ChildrenTransitions
		{
			get { return (TransitionCollection)this.GetValue(ChildrenTransitionsProperty); }
			set { this.SetValue(ChildrenTransitionsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Transitions.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ChildrenTransitionsProperty =
			DependencyProperty.Register("ChildrenTransitions", typeof(TransitionCollection), typeof(Panel), new PropertyMetadata(null, OnChildrenTransitionsChanged));

		private static void OnChildrenTransitionsChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var panel = dependencyObject as Panel;

			if (panel == null)
			{
				return;
			}

			if (panel._transitionHelper == null)
			{
				panel._transitionHelper = new PanelTransitionHelper(panel);
			}
		}

		#endregion

		#region Padding DependencyProperty

		internal Size BorderAndPaddingSize
		{
			get
			{
				var border = BorderThickness;
				var padding = Padding;
				var width = border.Left + border.Right + padding.Left + padding.Right;
				var height = border.Top + border.Bottom + padding.Top + padding.Bottom;
				return new Size(width, height);
			}
		}

		public Thickness Padding
		{
			get { return (Thickness)this.GetValue(PaddingProperty); }
			set { this.SetValue(PaddingProperty, value); }
		}

		public static readonly DependencyProperty PaddingProperty =
			DependencyProperty.Register(
				"Padding",
				typeof(Thickness),
				typeof(Panel),
				new FrameworkPropertyMetadata(
					Thickness.Empty,
					FrameworkPropertyMetadataOptions.None,
					(s, e) => ((Panel)s)?.OnPaddingChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
				)
			);

#endregion

#region BorderThickness DependencyProperty

		public Thickness BorderThickness
		{
			get { return (Thickness)this.GetValue(BorderThicknessProperty); }
			set { this.SetValue(BorderThicknessProperty, value); }
		}

		public static readonly DependencyProperty BorderThicknessProperty =
			DependencyProperty.Register(
				"BorderThickness",
				typeof(Thickness),
				typeof(Panel),
				new FrameworkPropertyMetadata(
					Thickness.Empty,
					FrameworkPropertyMetadataOptions.None,
					(s, e) => ((Panel)s)?.OnBorderThicknessChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
				)
			);

#endregion

#region BorderBrush Dependency Property

#if XAMARIN_ANDROID
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

		public static readonly DependencyProperty BorderBrushProperty =
			DependencyProperty.Register(
				"BorderBrush",
				typeof(Brush),
				typeof(Panel),
				new FrameworkPropertyMetadata(
					SolidColorBrushHelper.Transparent,
					FrameworkPropertyMetadataOptions.ValueInheritsDataContext,
					propertyChangedCallback: (s, e) => ((Panel)s).OnBorderBrushChanged((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);
#endregion

#region CornerRadius DependencyProperty

		public CornerRadius CornerRadius
		{
			get { return (CornerRadius)this.GetValue(CornerRadiusProperty); }
			set { this.SetValue(CornerRadiusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CornerRadius.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CornerRadiusProperty =
			DependencyProperty.Register(
				"CornerRadius",
				typeof(CornerRadius),
				typeof(Panel),
				new PropertyMetadata(
					CornerRadius.None,
					(s, e) => ((Panel)s)?.OnCornerRadiusChanged((CornerRadius)e.OldValue, (CornerRadius)e.NewValue)
				)
			);

#endregion

#region IsItemsHost DependencyProperty
		public static readonly DependencyProperty IsItemsHostProperty = DependencyProperty.Register(
			"IsItemsHost", typeof(bool), typeof(Panel), new PropertyMetadata(default(bool)));

		public bool IsItemsHost
		{
			get { return (bool)this.GetValue(IsItemsHostProperty); }
			private set { this.SetValue(IsItemsHostProperty, value); }
		}
		#endregion

		private ManagedWeakReference _itemsOwnerRef;

		internal ItemsControl ItemsOwner
		{
			get => _itemsOwnerRef.Target as ItemsControl;
			set
			{
				WeakReferencePool.ReturnWeakReference(this, _itemsOwnerRef);
				_itemsOwnerRef = WeakReferencePool.RentWeakReference(this, value);
			}
		}

		internal void SetItemsOwner(ItemsControl itemsOwner)
		{
			ItemsOwner = itemsOwner;
			IsItemsHost = itemsOwner != null;
		}

		protected virtual void OnCornerRadiusChanged(CornerRadius oldValue, CornerRadius newValue)
		{
			OnCornerRadiusChangedPartial(oldValue, newValue);
		}

		partial void OnCornerRadiusChangedPartial(CornerRadius oldValue, CornerRadius newValue);

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
	}
}
