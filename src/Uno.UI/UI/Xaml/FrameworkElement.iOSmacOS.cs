using CoreGraphics;
using Uno.Extensions;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Windows.Foundation;
using Uno.Foundation.Logging;
using Uno.UI;

#if __IOS__
using _View = UIKit.UIView;
using ObjCRuntime;
#elif __MACOS__
using _View = AppKit.NSView;
using ObjCRuntime;
#endif

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		private bool _inLayoutSubviews;
		private CGSize? _lastAvailableSize;
		private CGSize _lastMeasure;

		partial void Initialize();

		protected FrameworkElement()
		{
			Initialize();
		}

		partial void OnLoadedPartial()
		{
			ReconfigureViewportPropagationPartial();
		}

		private partial void ReconfigureViewportPropagationPartial();

		internal CGSize? XamlMeasure(CGSize availableSize)
		{
			if (((ILayouterElement)this).XamlMeasureInternal(availableSize, _lastAvailableSize, out var measuredSize))
			{
				_lastAvailableSize = availableSize;
				_lastMeasure = measuredSize;
				SetLayoutFlags(LayoutFlag.ArrangeDirty);
			}

			return _lastMeasure;
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

		protected Size SizeFromUISize(CGSize size)
		{
			var width = nfloat.IsNaN(size.Width) ? double.PositiveInfinity : (double)size.Width;
			var height = nfloat.IsNaN(size.Height) ? double.PositiveInfinity : (double)size.Height;

			return new Size(width, height).PhysicalToLogicalPixels();
		}

		protected Rect RectFromUIRect(CGRect rect)
		{
			var size = SizeFromUISize(rect.Size);
			var location = new Point(
				nfloat.IsNaN(rect.X) ? double.PositiveInfinity : (double)rect.X,
				nfloat.IsNaN(rect.Y) ? double.PositiveInfinity : (double)rect.Y);

			return new Rect(location.LogicalToPhysicalPixels(), size.LogicalToPhysicalPixels());
		}

		private bool IsTopLevelXamlView()
		{
			_View parent = this;
			while (parent != null)
			{
				parent = parent.Superview;
				if (parent is IFrameworkElement)
				{
					return false;
				}
			}
			return true;
		}
	}
}
