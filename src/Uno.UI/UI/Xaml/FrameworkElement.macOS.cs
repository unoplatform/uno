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
using AppKit;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		private bool _inLayoutSubviews;
		private CGSize? _lastAvailableSize;
		private CGSize _lastMeasure;

		partial void Initialize();

		internal bool RequiresArrange { get; private set; }

		internal bool RequiresMeasure { get; private set; }

		public override bool NeedsLayout
		{
			set
			{
				if (!_inLayoutSubviews)
				{
					base.NeedsLayout = value;
				}

				if (ShouldInterceptInvalidate)
				{
					return;
				}
			}
		}

		protected internal override void OnInvalidateMeasure()
		{
			base.OnInvalidateMeasure();

			// Note that the reliance on NSView.NeedsLayout to invalidate the measure / arrange phases for
			// self and parents.NeedsLayout is set to true when NSView.Frame is different from iOS, causing
			// a chain of multiple unneeded updates for the element and its parents. OnInvalidateMeasure
			// sets NeedsLayout to true and propagates to the parent but NeedsLayout by itself does not.

			if (!RequiresMeasure)
			{
				RequiresArrange = true;
				RequiresMeasure = true;

				if (Parent is FrameworkElement fe)
				{
					if (!fe.RequiresMeasure)
					{
						fe.InvalidateMeasure();
					}
				}
				else if (Parent is IFrameworkElement ife)
				{
					ife.InvalidateMeasure();
				}
			}
		}

		/// <summary>
		/// When set, measure and invalidate requests will not be propagated further up the visual tree, ie they won't trigger a relayout.
		/// Used where repeated unnecessary measure/arrange passes would be unacceptable for performance (eg scrolling in a list).
		/// </summary>
		internal bool ShouldInterceptInvalidate { get; set; }

		public FrameworkElement()
		{
			Initialize();
		}

		public override void Layout()
		{
			try
			{
				_inLayoutSubviews = true;

				if (RequiresMeasure)
				{
					XamlMeasure(Bounds.Size);
				}

				OnBeforeArrange();

				var size = SizeFromUISize(Bounds.Size);

				_layouter.Arrange(new Rect(0, 0, size.Width, size.Height));

				OnAfterArrange();
			}
			catch (Exception e)
			{
				this.Log().Error($"Layout failed in {GetType()}", e);
			}
			finally
			{
				_inLayoutSubviews = false;
				RequiresMeasure = false;
				RequiresArrange = false;
			}
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

		internal CGSize? XamlMeasure(CGSize availableSize)
		{
			// If set layout has not been called, we can 
			// return a previously cached result for the same available size.
			if (
				!RequiresMeasure
				&& _lastAvailableSize.HasValue
				&& availableSize == _lastAvailableSize
			)
			{
				return _lastMeasure;
			}

			_lastAvailableSize = availableSize;
			RequiresMeasure = false;

			var result = _layouter.Measure(SizeFromUISize(availableSize));

			result = IFrameworkElementHelper
				.SizeThatFits(this, result)
				.ToFoundationSize();

			return result.LogicalToPhysicalPixels();
		}

		public CGSize SizeThatFits(CGSize size)
		{
			try
			{
				_inLayoutSubviews = true;

				var xamlMeasure = XamlMeasure(size);

				if (xamlMeasure != null)
				{
					return _lastMeasure = xamlMeasure.Value;
				}
				else
				{
					return _lastMeasure = CGSize.Empty;
				}
			}
			finally
			{
				_inLayoutSubviews = false;
			}
		}

		protected Size SizeFromUISize(CGSize size)
		{
			var width = nfloat.IsNaN(size.Width) ? float.PositiveInfinity : size.Width;
			var height = nfloat.IsNaN(size.Height) ? float.PositiveInfinity : size.Height;

			return new Size(width, height).PhysicalToLogicalPixels();
		}

		private bool IsTopLevelXamlView()
		{
			NSView parent = this;
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
