using CoreGraphics;
using Uno.Extensions;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Windows.Foundation;
using Uno.Logging;
using Uno.UI;

#if __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#endif

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		private bool _inLayoutSubviews;
		private CGSize? _lastAvailableSize;
		private CGSize _lastMeasure;

		partial void Initialize();

		public FrameworkElement()
		{
			Initialize();
		}

		internal CGSize? XamlMeasure(CGSize availableSize)
		{
			// If set layout has not been called, we can 
			// return a previously cached result for the same available size.
			if (
				!IsMeasureDirty
				&& _lastAvailableSize.HasValue
				&& availableSize == _lastAvailableSize
			)
			{
				return _lastMeasure;
			}

			_lastAvailableSize = availableSize;
			IsMeasureDirty = false;

			var result = _layouter.Measure(SizeFromUISize(availableSize));

			// Result here exclude the margins on the element
			return _lastMeasure = result.LogicalToPhysicalPixels();
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
			var width = nfloat.IsNaN(size.Width) ? float.PositiveInfinity : size.Width;
			var height = nfloat.IsNaN(size.Height) ? float.PositiveInfinity : size.Height;

			return new Size(width, height).PhysicalToLogicalPixels();
		}

		protected Rect RectFromUIRect(CGRect rect)
		{
			var size = SizeFromUISize(rect.Size);
			var location = new Point(
				nfloat.IsNaN(rect.X) ? float.PositiveInfinity : rect.X,
				nfloat.IsNaN(rect.Y) ? float.PositiveInfinity : rect.Y);

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
