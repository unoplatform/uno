using CoreGraphics;
using Uno.Extensions;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using System.ComponentModel;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.UI.Xaml.Controls.Layouter;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement
	{
		public override void SetNeedsLayout()
		{
			base.SetNeedsLayout();
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			// If we get LayoutSubviews on managed element, it could be because a native child measure has changed
			// In this case, parents aren't automagically invalidated. For example, while typing in TextBox in a way that makes it grow.
			// So, once we get LayoutSubviews, we go through the native-only children. If we found any of them has
			// changed, we do invalidate the current managed element (the parent of the native-only child)
			//foreach (var child in this.GetChildren())
			//{
			//	if (child is UIView nativeOnlyChild && child is not UIElement)
			//	{
			//		var oldDesiredSize = LayoutInformation.GetDesiredSize(nativeOnlyChild);
			//		var desiredSize = (Size)nativeOnlyChild.SizeThatFits(LayoutInformation.GetAvailableSize(nativeOnlyChild));
			//		if (oldDesiredSize != desiredSize)
			//		{
			//			LayoutInformation.SetDesiredSize(nativeOnlyChild, desiredSize);
			//			this.InvalidateMeasure();
			//			this.InvalidateArrange();
			//		}
			//	}
			//}
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			if (IsVisualTreeRoot)
			{
				return base.SizeThatFits(size);
			}

			this.Measure(size);
			return this.DesiredSize;
		}

		public override void AddSubview(UIView view)
		{
			if (IsLoaded)
			{
				// Apply styles in the subtree being loaded (if not already applied). We do it in this way to force Styles application in a
				// 'root-first' order, because on iOS the native loading callback is raised 'leaf first,' and waiting until this point to
				// apply the style can cause Loading/Loaded to be raised twice for some views (because template of outer control changes).
				//
				// This override can be removed when Loading/Loaded timing is adjusted to fully match UWP.
				if (view is IDependencyObjectStoreProvider provider)
				{
					// Set parent so implicit styles in the tree can be resolved
					provider.Store.Parent = this;
				}
				ApplyStylesToChildren(view);
			}
			base.AddSubview(view);

			void ApplyStylesToChildren(UIView viewInner)
			{
				if (viewInner is FrameworkElement fe)
				{
					fe.ApplyStyles();
				}

				foreach (var subview in viewInner.Subviews)
				{
					ApplyStylesToChildren(subview);
				}
			}
		}
	}
}
