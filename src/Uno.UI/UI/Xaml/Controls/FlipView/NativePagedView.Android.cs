using System;
using System.Collections.Generic;
using System.Text;
using AndroidX.ViewPager.Widget;
using Android.Views;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NativePagedView : ViewPager, DependencyObject, ILayoutConstraints, ILayouterElement
	{
		public NativePagedView() : base(ContextHelper.Current)
		{
			Initialize();
		}

		private void Initialize()
		{
			InitializeBinder();
			LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
		}

		bool ILayouterElement.StretchAffectsMeasure => false;

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}

		Size ILayouterElement.Measure(Size availableSize)
		{
			var sizeThatFits = IFrameworkElementHelper.SizeThatFits(this, availableSize);

			this.Measure(ViewHelper.SpecFromLogicalSize(sizeThatFits.Width), ViewHelper.SpecFromLogicalSize(sizeThatFits.Height));

			double maxChildWidth = 0f, maxChildHeight = 0f;

			//Per the link, if the NativePagedView has no fixed size in a dimension, wrap it to the size of its largest child. - http://stackoverflow.com/questions/8394681/android-i-am-unable-to-have-viewpager-wrap-content
			//This might be brittle if items have varying dimensions along the unfixed axis; one (hackish) solution would be to increase OffscreenPageLimit (the number of offscreen pages that are kept).
			foreach (var child in this.GetChildren())
			{
				var desiredChildSize = MobileLayoutingHelpers.MeasureElement(child, availableSize);
				maxChildWidth = Math.Max(maxChildWidth, desiredChildSize.Width);
				maxChildHeight = Math.Max(maxChildHeight, desiredChildSize.Height);
			}

			return new Size(
				!double.IsPositiveInfinity(availableSize.Width) ? availableSize.Width : maxChildWidth,
				!double.IsPositiveInfinity(availableSize.Height) ? availableSize.Height : maxChildHeight
			);
		}

		void ILayouterElement.Arrange(Rect finalRect)
		{
			foreach (var child in this.GetChildren())
			{
				MobileLayoutingHelpers.ArrangeElement(child, finalRect);
			}

			var physical = finalRect.LogicalToPhysicalPixels();
			this.Layout((int)physical.Left, (int)physical.Top, (int)physical.Right, (int)physical.Bottom);
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
	}
}
