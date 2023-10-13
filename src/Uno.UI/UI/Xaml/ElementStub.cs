#if IS_UNO
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.Foundation;

#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = System.Object;
#endif

namespace Windows.UI.Xaml
{

	/// <summary>
	/// A support element for the DeferLoadStrategy Lazy Xaml directive.
	/// </summary>
	/// <remarks>This control is added in the visual tree, in place of the original content.</remarks>
	public partial class ElementStub : FrameworkElement
	{
#if UNO_HAS_UIELEMENT_IMPLICIT_PINNING
		ManagedWeakReference _contentReference;

		private View _content
		{
			get => _contentReference?.Target as View;
			set
			{
				if (_contentReference != null)
				{
					WeakReferencePool.ReturnWeakReference(this, _contentReference);
				}

				_contentReference = WeakReferencePool.RentWeakReference(this, value);
			}
		}
#else
		private View _content;
#endif

		/// <summary>
		/// Ensures that materialization handles reentrancy properly.
		/// This scenario can happen on android specifically because the Parent
		/// property is not immediately set to null once a view is removed from
		/// the tree.
		/// </summary>
		private bool _isMaterializing;

		/// <summary>
		/// A delegate used to raise materialization changes in <see cref="ElementStub.MaterializationChanged"/>
		/// </summary>
		/// <param name="sender">The instance being changed</param>
		public delegate void MaterializationChangedHandler(ElementStub sender);

		/// <summary>
		/// An event raised when the materialized object of the <see cref="ElementStub"/> has changed.
		/// </summary>
		public event MaterializationChangedHandler MaterializationChanged;

		/// <summary>
		/// A delegate used to signal that the content is being materialized in <see cref="ElementStub.Materializing"/>
		/// </summary>
		/// <param name="sender">The instance being changed</param>
		public delegate void MaterializingChangedHandler(ElementStub sender);

		/// <summary>
		/// An event raised when the materialized object of the <see cref="ElementStub"/> has changed.
		/// </summary>
		/// <remarks>
		/// This event is only raised when the ElementStub is materializing its target (not
		/// dematerializing), and is raised after the element stub has been removed from the
		/// tree, but before the new target is added to the tree (so the x:Bind on loaded event
		/// can be raised properly).
		/// </remarks>
		public event MaterializationChangedHandler Materializing;

		public bool Load
		{
			get => (bool)GetValue(LoadProperty);
			set => SetValue(LoadProperty, value);
		}

		// Using a DependencyProperty as the backing store for Load.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LoadProperty =
			DependencyProperty.Register("Load", typeof(bool), typeof(ElementStub), new FrameworkPropertyMetadata(
				false, OnLoadChanged));

		/// <summary>
		/// Determines if the current ElementStub has been materialized to its target View.
		/// </summary>
		public bool IsMaterialized => _content != null;

		public ElementStub(Func<View> contentBuilder) : this()
		{
#if UNO_HAS_UIELEMENT_IMPLICIT_PINNING
			// In this context, the delegate provided here closes over other UIElement instances
			// causing memory leaks unless the Target member of the delegate is weak.
			// Here, we deconstruct the delegate to keep the MethodInfo, and invoking it
			// via the resolution of the weak reference. This technique only works because
			// the provided delegate and its target (the lambda's display class) is kept
			// alive by the "ElementStub holder" variable provided by the XAML generator.
			var delegateTarget = WeakReferencePool.RentWeakReference(this, contentBuilder.Target);
			var methodInfo = contentBuilder.Method;

			ContentBuilder = () => (View)methodInfo.Invoke(delegateTarget.Target, null);
#else
			ContentBuilder = contentBuilder;
#endif
		}

		public ElementStub()
		{
			Visibility = Visibility.Collapsed;
		}

		private static void OnLoadChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if ((bool)args.NewValue)
			{
				((ElementStub)dependencyObject).Materialize();
			}
			else
			{
				((ElementStub)dependencyObject).Dematerialize();
			}
		}


		/// <summary>
		/// A function that will create the actual view.
		/// </summary>
		public Func<View> ContentBuilder { get; set; }

		protected override Size MeasureOverride(Size availableSize)
			=> MeasureFirstChild(availableSize);

		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeFirstChild(finalSize);

		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			base.OnVisibilityChanged(oldValue, newValue);

			if (ContentBuilder != null
				&& oldValue == Visibility.Collapsed
				&& newValue == Visibility.Visible
				&& Parent != null
			)
			{
				Materialize(isVisibilityChanged: true);
			}
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			if (ContentBuilder != null
				&& Visibility == Visibility.Visible
			)
			{
				Materialize();
			}
		}

		public void Materialize()
			=> Materialize(isVisibilityChanged: false);

		private void RaiseMaterializing()
		{
			if (_isMaterializing)
			{
				Materializing?.Invoke(this);
			}
		}

		private void Materialize(bool isVisibilityChanged)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"ElementStub.Materialize(isVibilityChanged: {isVisibilityChanged})");
			}

			if (_content == null && !_isMaterializing)
			{
				try
				{
					_isMaterializing = true;

					_content = SwapViews(oldView: (FrameworkElement)this, newViewProvider: ContentBuilder);
					var targetDependencyObject = _content as DependencyObject;

					if (isVisibilityChanged && targetDependencyObject != null)
					{
						var visibilityProperty = GetVisibilityProperty(_content);

						// Set the visibility at the same precedence it was currently set with on the stub.
						var precedence = this.GetCurrentHighestValuePrecedence(visibilityProperty);

						targetDependencyObject.SetValue(visibilityProperty, Visibility.Visible, precedence);
					}

					MaterializationChanged?.Invoke(this);
				}
				finally
				{
					_isMaterializing = false;
				}
			}
		}

		private void Dematerialize()
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"ElementStub.Dematerialize()");
			}

			if (_content != null)
			{
				var newView = SwapViews(oldView: (FrameworkElement)_content, newViewProvider: () => this as View);
				if (newView != null)
				{
					_content = null;
				}

				MaterializationChanged?.Invoke(this);
			}
		}

		private static DependencyProperty GetVisibilityProperty(View view)
		{
			if (view is FrameworkElement)
			{
				return VisibilityProperty;
			}
			else
			{
				return DependencyProperty.GetProperty(view.GetType(), nameof(Visibility));
			}
		}
	}
}
#endif
