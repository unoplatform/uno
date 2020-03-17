using Android.Views;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Windows.Foundation;
using Uno.UI.Services;
using Uno.Diagnostics.Eventing;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		private Size? _lastLayoutSize;
		private Size _actualSize;

		internal Size ActualSize => _actualSize;

		/// <summary>
		/// The parent of the <see cref="FrameworkElement"/> in the visual tree, which may differ from its <see cref="Parent"/> (ie if it's a child of a native view).
		/// </summary>
		internal IViewParent VisualParent => (this as View).Parent;

		public FrameworkElement()
		{
			Initialize();
		}

		partial void Initialize();

		protected override void OnNativeLoaded()
		{
			try
			{
				PerformOnLoaded();

				base.OnNativeLoaded();
			}
			catch (Exception ex)
			{
				this.Log().Error("OnNativeLoaded failed in FrameworkElementMixins", ex);
				Application.Current.RaiseRecoverableUnhandledException(ex);
			}
		}

		private void PerformOnLoaded()
		{
			((IDependencyObjectStoreProvider)this).Store.Parent = base.Parent;
			OnLoading();
			OnLoaded();

			if (FeatureConfiguration.FrameworkElement.AndroidUseManagedLoadedUnloaded)
			{
				foreach (var child in (this as IShadowChildrenProvider).ChildrenShadow)
				{
					if (child is FrameworkElement e)
					{
						// Mark this instance as managed loaded through managed children
						// traversal, to avoid paying the cost of overridden method interop
						e.IsManagedLoaded = true;

						// Calling this method is acceptable as it is an abstract method that
						// will never do interop with the java class. It is required to invoke
						// Loaded/Unloaded actions.
						e.OnNativeLoaded();
					}
				}
			}
		}

		protected override void OnNativeUnloaded()
		{
			try
			{
				PerformOnUnloaded();

				base.OnNativeUnloaded();
			}
			catch (Exception ex)
			{
				this.Log().Error("OnNativeUnloaded failed in FrameworkElementMixins", ex);
				Application.Current.RaiseRecoverableUnhandledException(ex);
			}
		}

		internal void PerformOnUnloaded()
		{
			if (FeatureConfiguration.FrameworkElement.AndroidUseManagedLoadedUnloaded)
			{
				if (IsNativeLoaded)
				{
					OnUnloaded();

					void ProcessView(View view)
					{
						if (view is FrameworkElement e)
						{
							// Mark this instance as managed loaded through managed children
							// traversal, to avoid paying the cost of overridden method interop
							e.IsManagedLoaded = false;

							// Calling this method is acceptable as it is an abstract method that
							// will never do interop with the java class. It is required to invoke
							// Loaded/Unloaded actions.
							e.OnNativeUnloaded();
						}
						else if (view is ViewGroup childViewGroup)
						{
							// If the child is a non-UnoView group,
							// search its children for uno viewgroups.
							TraverseChildren(childViewGroup);
						}
					}

					void TraverseChildren(ViewGroup viewGroup)
					{
						if (viewGroup is IShadowChildrenProvider shadowList)
						{
							// Allocation-less enumeration
							foreach (var child in shadowList.ChildrenShadow)
							{
								ProcessView(child);
							}
						}
						else
						{
							foreach (var child in viewGroup.GetChildren())
							{
								ProcessView(child);
							}
						}
					}

					TraverseChildren(this);
				}
			}
			else
			{
				OnUnloaded();
			}
		}

		/// <summary>
		/// Notifies that this view has been removed from its parent. This method is only 
		/// called when the parent is an UnoViewGroup.
		/// </summary>
		protected override void OnRemovedFromParent()
		{
			base.OnRemovedFromParent();

			((IDependencyObjectStoreProvider)this).Store.Parent = null;
		}

		partial void OnLoadedPartial()
		{
			// see StretchAffectsMeasure for details.
			this.SetValue(
				StretchAffectsMeasureProperty,
				!(VisualParent is DependencyObject),
				DependencyPropertyValuePrecedences.DefaultValue
			);
		}

		#region StretchAffectsMeasure DependencyProperty

		/// <summary>
		/// Indicates whether stretching (HorizontalAlignment.Stretch and VerticalAlignment.Stretch) should affect the measured size of the FrameworkElement.
		/// Only set on a FrameworkElement if the parent is a native view whose layouting relies on the values of MeasuredWidth and MeasuredHeight to account for stretching.
		/// Note that this doesn't take Margins into account.
		/// </summary>
		/// <remarks>
		/// The <see cref="DependencyPropertyValuePrecedences.DefaultValue"/> is updated at each <see cref="OnLoadedPartial"/> call, but may
		/// be overridden by an external called as <see cref="DependencyPropertyValuePrecedences.Local"/>.
		/// </remarks>
		public bool StretchAffectsMeasure
		{
			get { return (bool)GetValue(StretchAffectsMeasureProperty); }
			set { SetValue(StretchAffectsMeasureProperty, value); }
		}

		// Using a DependencyProperty as the backing store for StretchAffectsMeasure.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StretchAffectsMeasureProperty =
			DependencyProperty.Register("StretchAffectsMeasure", typeof(bool), typeof(FrameworkElement), new PropertyMetadata(false));

		#endregion

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			var availableSize = ViewHelper.LogicalSizeFromSpec(widthMeasureSpec, heightMeasureSpec);

			var measuredSizelogical = _layouter.Measure(availableSize);

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat(
					"[{0}/{1}] OnMeasure1({2}, {3}) (parent: {4}/{5})",
					GetType(),
					Name,
					measuredSizelogical.Width,
					measuredSizelogical.Height,
					ViewHelper.MeasureSpecGetSize(widthMeasureSpec),
					ViewHelper.MeasureSpecGetSize(heightMeasureSpec)
				);
			}

			var measuredSize = measuredSizelogical.LogicalToPhysicalPixels();

			if (StretchAffectsMeasure)
			{
				if (HorizontalAlignment == HorizontalAlignment.Stretch && !double.IsPositiveInfinity(availableSize.Width))
				{
					measuredSize.Width = ViewHelper.MeasureSpecGetSize(widthMeasureSpec);
				}

				if (VerticalAlignment == VerticalAlignment.Stretch && !double.IsPositiveInfinity(availableSize.Height))
				{
					measuredSize.Height = ViewHelper.MeasureSpecGetSize(heightMeasureSpec);
				}
			}

			// Report our final dimensions.
			SetMeasuredDimension(
				(int)measuredSize.Width,
				(int)measuredSize.Height
			);
		}

		protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayoutCore(changed, left, top, right, bottom);

			Size newSize;
			if (ArrangeLogicalSize is Rect als)
			{
				// If the parent element is from managed code,
				// we can recover the "Arrange" with double accuracy.
				// We use that because the conversion to android's "int" is loosing too much precision.
				newSize = new Size(als.Width, als.Height);
			}
			else
			{
				// Here the "arrange" is coming from a native element,
				// so we convert those measurements to logical ones.
				newSize = new Size(right - left, bottom - top).PhysicalToLogicalPixels();
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat(
					"[{0}/{1}] OnLayoutCore({2}, {3}, {4}, {5}) (parent: {5},{6})",
					GetType(),
					Name,
					left, top, right, bottom,
					MeasuredWidth,
					MeasuredHeight
				);
			}

			var previousSize = _actualSize;
			_actualSize = newSize;

			if (
				// If the layout has changed, but the final size has not, this is just a translation.
				// So unless there was a layout requested, we can skip arranging the children.
				(changed && _lastLayoutSize != newSize)

				// Even if nothing changed, but a layout was requested, arrange the children.
				|| IsLayoutRequested
			)
			{
				_lastLayoutSize = newSize;

				var finalRect = new Rect(0, 0, newSize.Width, newSize.Height);

				OnBeforeArrange();

				_layouter.Arrange(finalRect);

				OnAfterArrange();
			}

			if (previousSize != newSize)
			{
				SizeChanged?.Invoke(this, new SizeChangedEventArgs(this, previousSize, newSize));
				_renderTransform?.UpdateSize(newSize);
			}
		}

		/// <summary>
		/// Provides an implementation <see cref="ViewGroup.Layout(int, int, int, int)"/> in order 
		/// to avoid the back and forth between Java and C#.
		/// </summary>
		internal void FastLayout(bool changed, int left, int top, int right, int bottom)
		{
			try
			{
				// Flag the native UnoViewGroup so it does not call OnLayoutCore because we're
				// calling it from the Uno side.
				NativeStartLayoutOverride(left, top, right, bottom);

				// Invoke our own layouting without going back and fort with Java.
				OnLayoutCore(changed, left, top, right, bottom);
			}
			finally
			{
				// Invoke the real layout method.
				NativeFinishLayoutOverride();
			}
		}

		protected override bool NativeRequestLayout()
		{
			if (!base.NativeRequestLayout())
			{
				return false;
			}

			if (_trace.IsEnabled && !IsLayoutRequested)
			{
				_trace.WriteEvent(
					FrameworkElement.TraceProvider.FrameworkElement_InvalidateMeasure,
					EventOpcode.Send,
					new[] {
						GetType().ToString(),
						this.GetDependencyObjectId().ToString()
					}
				);
			}

			if (!ShouldPropagateLayoutRequest())
			{
				//This view and the visual tree above it won't change size. Send the view to the LayoutManager to be remeasured and rearranged.
				if (!IsLayoutRequested)
				{
					LayoutManager.InvalidateArrange(this);
					// Call ForceLayout, otherwise View.measure() & View.layout() won't do anything
					this.ForceLayout();
				}
				return false;
			}

			_constraintsChanged = false;
			return true;
		}

		private bool IsTopLevelXamlView()
		{
			IViewParent parent = this;
			while (parent != null)
			{
				parent = parent.Parent;
				if (parent is IFrameworkElement)
				{
					return false;
				}
			}
			// If Parent == null, this is probably a not-yet-attached/already-detached view rather than the top-level view
			return (this as IViewParent).Parent != null;
		}

		/// <summary>
		/// Called before Arrange is called, this method will be deprecated
		/// once OnMeasure/OnArrange will be implemented completely
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected virtual void OnBeforeArrange()
		{

		}

		/// <summary>
		/// Called after Arrange is called, this method will be deprecated
		/// once OnMeasure/OnArrange will be implemented completely
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected virtual void OnAfterArrange()
		{

		}
	}
}
