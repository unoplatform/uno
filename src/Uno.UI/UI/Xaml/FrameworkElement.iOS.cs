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
						// Add back the Margin (which is normally 'outside' the view's bounds) - the layouter will subtract it again
						XamlMeasure(Bounds.Size.Add(Margin));
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
