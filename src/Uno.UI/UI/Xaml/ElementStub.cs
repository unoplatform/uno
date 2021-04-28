#if IS_UNO
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Uno.UI;
using Uno.UI.DataBinding;

#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
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
		[Weak]
#endif
		private View _content;

		/// <summary>
		/// A delegate used to raise materialization changes in <see cref="ElementStub.MaterializationChanged"/>
		/// </summary>
		/// <param name="sender">The instance being changed</param>
		public delegate void MaterializationChangedHandler(ElementStub sender);

		/// <summary>
		/// An event raised when the materialized object of the <see cref="ElementStub"/> has changed.
		/// </summary>
		public event MaterializationChangedHandler MaterializationChanged;

		public bool Load
		{
			get => (bool)GetValue(LoadProperty);
			set => SetValue(LoadProperty, value);
		}

		// Using a DependencyProperty as the backing store for Load.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LoadProperty =
			DependencyProperty.Register("Load", typeof(bool), typeof(ElementStub), new PropertyMetadata(
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

		private void Materialize(bool isVisibilityChanged)
		{
			if (_content == null)
			{
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
		}

		private void Dematerialize()
		{
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
