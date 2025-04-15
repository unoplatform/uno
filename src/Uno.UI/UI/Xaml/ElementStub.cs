﻿#if IS_UNO
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Windows.Foundation;

#if __ANDROID__
using View = Android.Views.View;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
#else
using View = System.Object;
#endif

namespace Microsoft.UI.Xaml
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

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
		private bool _fromLegacyTemplate;
#endif

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

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
			_fromLegacyTemplate = TemplatedParentScope.GetCurrentTemplate() is { IsLegacyTemplate: true };
#endif
		}

		public ElementStub()
		{
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

#if UNO_HAS_ENHANCED_LIFECYCLE
		internal override void EnterImpl(EnterParams @params, int depth)
		{
			// the base impl would cause immediately materialization by loading this stub
			// which is not something we want here.
		}

		internal override void LeaveImpl(LeaveParams @params)
		{
			// do nothing
		}
#endif

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			if (ContentBuilder != null && Load)
			{
				Materialize();
			}
		}

		public void Materialize()
			=> MaterializeInner();

		private void RaiseMaterializing()
		{
			if (_isMaterializing)
			{
				Materializing?.Invoke(this);
			}
		}

		private void MaterializeInner()
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"ElementStub.Materialize()");
			}

			if (_content == null && !_isMaterializing)
			{
				try
				{
					_isMaterializing = true;
#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
					TemplatedParentScope.PushScope(GetTemplatedParent(), _fromLegacyTemplate);
#endif

					// Before swapping out the ElementStub, we set the inherited DC locally so that when removed from the
					// visual tree, the inherited DC is kept intact. This is important in case of setting x:Load with an
					// x:Bind. The binding we generate from the x:Bind will only listen to property changes while the DC
					// is present. This is definitely not what happens on WinUI, but considering our implementation of
					// x:Bind and ElementStub differ completely from WinUI's, this is acceptable for now.
					// https://github.com/unoplatform/uno/issues/18509
					if ((this as IDependencyObjectStoreProvider).Store.GetPropertyDetails(DataContextProperty)
						.CurrentHighestValuePrecedence > DependencyPropertyValuePrecedences.Local)
					{
						this.SetValue(DataContextProperty, DataContext, DependencyPropertyValuePrecedences.Local);
					}

					_content = SwapViews(oldView: (FrameworkElement)this, newViewProvider: ContentBuilder);

					MaterializationChanged?.Invoke(this);
				}
				finally
				{
#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
					TemplatedParentScope.PopScope();
#endif
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
				this.ClearValue(DataContextProperty, DependencyPropertyValuePrecedences.Local);
				var newView = SwapViews(oldView: (FrameworkElement)_content, newViewProvider: () => this as View);
				if (newView != null)
				{
					_content = null;
				}

				MaterializationChanged?.Invoke(this);
			}
		}
	}
}
#endif
