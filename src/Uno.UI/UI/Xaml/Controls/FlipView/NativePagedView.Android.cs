using System;
using System.Collections.Generic;
using System.Text;
using AndroidX.ViewPager.Widget;
using Android.Views;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Controls;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativePagedView : ViewPager, DependencyObject, ILayoutConstraints, ILayouterElement
	{
		private Size _lastLayoutSize;

		public NativePagedView() : base(ContextHelper.Current)
		{
			Initialize();
		}

		private void Initialize()
		{
			InitializeBinder();
			LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			_layouter = new NativePagedViewLayouter(this);
		}

		private ILayouter _layouter;

		ILayouter ILayouterElement.Layouter => _layouter;
		Size ILayouterElement.LastAvailableSize => LayoutInformation.GetAvailableSize(this);
		bool ILayouterElement.IsMeasureDirty => true;
		bool ILayouterElement.IsFirstMeasureDoneAndManagedElement => false;
		bool ILayouterElement.StretchAffectsMeasure => false;
		bool ILayouterElement.IsMeasureDirtyPathDisabled => true;

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			var measuredSize = ((ILayouterElement)this).OnMeasureInternal(widthMeasureSpec, heightMeasureSpec);

			var logicalMeasuredSize = measuredSize.PhysicalToLogicalPixels();

			//We call ViewPager.OnMeasure here, because it creates the page views.
			base.OnMeasure(
				ViewHelper.SpecFromLogicalSize(logicalMeasuredSize.Width),
				ViewHelper.SpecFromLogicalSize(logicalMeasuredSize.Height)
			);

			IFrameworkElementHelper.OnMeasureOverride(this);
		}

		void ILayouterElement.SetMeasuredDimensionInternal(int width, int height)
		{
			SetMeasuredDimension(width, height);
		}

		//TODO generated code
		partial void OnLayoutPartial(bool changed, int left, int top, int right, int bottom)
		{
			var newSize = new Size(right - left, bottom - top).PhysicalToLogicalPixels();

			if (
				// If the layout has changed, but the final size has not, this is just a translation.
				// So unless there was a layout requested, we can skip arranging the children.
				(changed && _lastLayoutSize != newSize)

				// Even if nothing changed, but a layout was requested, arrange the children.
				|| IsLayoutRequested
			)
			{
				_lastLayoutSize = newSize;

				_layouter.Arrange(new global::Windows.Foundation.Rect(0, 0, newSize.Width, newSize.Height));
			}
		}

		public override void AddView(View child)
		{
			base.AddView(child);
		}

		public override void RemoveView(View view)
		{
			base.RemoveView(view);
			(view as BindableView)?.NotifyRemovedFromParent();
		}

		public override void RemoveViewAt(int index)
		{
			var view = GetChildAt(index);
			base.RemoveViewAt(index);
			(view as BindableView)?.NotifyRemovedFromParent();
		}

		public bool IsWidthConstrained(View requester) => (Parent as ILayoutConstraints)?.IsWidthConstrained(this) ?? false;

		public bool IsHeightConstrained(View requester) => (Parent as ILayoutConstraints)?.IsHeightConstrained(this) ?? false;

		private class NativePagedViewLayouter : Layouter
		{

			public NativePagedViewLayouter(NativePagedView view) : base(view) { }

			protected override string Name => Panel.Name;


			protected override void MeasureChild(View view, int widthSpec, int heightSpec)
			{
				(Panel as NativePagedView).MeasureChild(view, widthSpec, heightSpec);
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				//Do nothing here. FrameworkElementMixins.OnLayout calls ViewPager.OnLayout, which lays out its visible page.
				return finalSize;
			}

			protected override Size MeasureOverride(Size availableSize)
			{
				var sizeThatFits = IFrameworkElementHelper.SizeThatFits(Panel, availableSize);

				double maxChildWidth = 0f, maxChildHeight = 0f;

				//Per the link, if the NativePagedView has no fixed size in a dimension, wrap it to the size of its largest child. - http://stackoverflow.com/questions/8394681/android-i-am-unable-to-have-viewpager-wrap-content
				//This might be brittle if items have varying dimensions along the unfixed axis; one (hackish) solution would be to increase OffscreenPageLimit (the number of offscreen pages that are kept).
				if (double.IsPositiveInfinity(sizeThatFits.Width) || double.IsPositiveInfinity(sizeThatFits.Height))
				{
					foreach (var child in this.GetChildren())
					{
						var desiredChildSize = MeasureChild(child, sizeThatFits);
						maxChildWidth = Math.Max(maxChildWidth, desiredChildSize.Width);
						maxChildHeight = Math.Max(maxChildHeight, desiredChildSize.Height);
					}
				}

				return new Size(
					!double.IsPositiveInfinity(sizeThatFits.Width) ? sizeThatFits.Width : maxChildWidth,
					!double.IsPositiveInfinity(sizeThatFits.Height) ? sizeThatFits.Height : maxChildHeight
				);
			}
		}
	}
}
