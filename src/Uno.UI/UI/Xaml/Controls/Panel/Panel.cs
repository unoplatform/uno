using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using View = UIKit.UIView;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	[Markup.ContentProperty(Name = "Children")]
	public partial class Panel : FrameworkElement, IPanel
#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		, ICustomClippingElement
#endif
	{
		private readonly BorderLayerRenderer _borderRenderer;

#if IS_UNIT_TESTS || UNO_REFERENCE_API
		private new UIElementCollection _children;
#else
		private UIElementCollection _children;
#endif

		private PanelTransitionHelper _transitionHelper;

		public Panel()
		{
			_borderRenderer = new BorderLayerRenderer(this);
			_children = new UIElementCollection(this);
		}

		private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
			=> OnChildrenChanged();

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			_children.CollectionChanged += OnChildrenCollectionChanged;

			OnLoadedPartial();
		}

		partial void OnLoadedPartial();

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_children.CollectionChanged -= OnChildrenCollectionChanged;

			OnUnloadedPartial();
		}

		partial void OnUnloadedPartial();

		private protected virtual void OnChildAdded(IFrameworkElement element)
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
		public static DependencyProperty ChildrenTransitionsProperty { get; } =
			DependencyProperty.Register("ChildrenTransitions", typeof(TransitionCollection), typeof(Panel), new FrameworkPropertyMetadata(null, OnChildrenTransitionsChanged));

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

		/// <summary>
		/// This corresponds to WinUI's IOrientedPanel::get_PhysicalOrientation, but its overrides
		/// are not yet ported from WinUI. Generally speaking, the override should point to
		/// the corresponding Orientation property in the subclass (e.g. StackPanel.Orientation)
		/// </summary>
		internal virtual Orientation? PhysicalOrientation { get; }

		internal Thickness PaddingInternal { get; set; }

		internal Thickness BorderThicknessInternal { get; set; }

		private Brush _borderBrushInternal;

		internal Brush BorderBrushInternal
		{
			get => _borderBrushInternal;
			set
			{
#if __WASM__
				if (((_borderBrushInternal is null) ^ (value is null)) && BorderThicknessInternal != default)
				{
					// The transition from null to non-null (and vice-versa) affects child arrange on Wasm when non-zero BorderThickness is specified.
					foreach (var child in _children)
					{
						child.InvalidateArrange();
					}
				}
#endif

				_borderBrushInternal = value;
			}
		}

		internal CornerRadius CornerRadiusInternal { get; set; }

		#region IsItemsHost DependencyProperty
		public static DependencyProperty IsItemsHostProperty { get; } = DependencyProperty.Register(
			"IsItemsHost", typeof(bool), typeof(Panel), new FrameworkPropertyMetadata(default(bool)));

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

		private protected override Thickness GetBorderThickness() => BorderThicknessInternal;

		internal override bool CanHaveChildren() => true;


		private protected void OnBackgroundSizingChangedInnerPanel(DependencyPropertyChangedEventArgs e)
		{
			base.OnBackgroundSizingChangedInner(e);

			UpdateBorder();
		}

		partial void UpdateBorder();

		internal override bool IsViewHit() => Border.IsViewHitImpl(this);
	}
}
