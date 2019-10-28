using CoreGraphics;
using Uno.Extensions;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using System.ComponentModel;
using Windows.Foundation;
using Uno.Logging;
using Uno.UI;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		private bool _inLayoutSubviews;
		private CGSize? _lastAvailableSize;
		private CGSize _lastMeasure;

		partial void Initialize();

		public override void SetNeedsLayout()
		{
			if (!_inLayoutSubviews)
			{
				base.SetNeedsLayout();
			}

			RequiresMeasure = true;
			RequiresArrange = true;

			SetSuperviewNeedsLayout();
		}

		public FrameworkElement()
		{
			Initialize();
		}

		public override void LayoutSubviews()
		{
			if (Visibility == Visibility.Collapsed)
			{
				// //Don't layout collapsed views
				return;
			}

			try
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
				finally
				{
					_inLayoutSubviews = false;
					RequiresArrange = false;
				}
			}
			catch (Exception e)
			{
				this.Log().Error($"Layout failed in {GetType()}", e);
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

		private CGSize? XamlMeasure(CGSize availableSize)
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

		public override CGSize SizeThatFits(CGSize size)
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
					return _lastMeasure = base.SizeThatFits(size);
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
			UIView parent = this;
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
