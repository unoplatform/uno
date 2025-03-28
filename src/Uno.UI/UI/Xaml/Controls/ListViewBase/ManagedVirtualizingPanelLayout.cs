#if __ANDROID__ || __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	abstract partial class ManagedVirtualizingPanelLayout : DependencyObject
	{
		// Copied from VirtualizingPanelLayout.cs

		/// <summary>
		/// The direction of scroll.
		/// </summary>
		public abstract Orientation ScrollOrientation { get; }

		/// <summary>
		/// Whether the content should be stretched in breadth (ie perpendicular to the direction of scroll).
		/// </summary>
		public bool ShouldBreadthStretch
		{
			get
			{
				if (XamlParent == null)
				{
					return true;
				}

				if (IsInsidePopup)
				{
					return false;
				}

				if (ScrollOrientation == Orientation.Vertical)
				{
					return XamlParent.HorizontalAlignment == HorizontalAlignment.Stretch;
				}
				else
				{
					return XamlParent.VerticalAlignment == VerticalAlignment.Stretch;
				}
			}
		}
		/// <summary>
		/// Determines if the owner Panel is inside a popup. Used to determine
		/// if the computation of the breadth should be using the parent's stretch
		/// modes.
		/// Related: https://github.com/unoplatform/uno/issues/135
		/// </summary>
		private bool IsInsidePopup { get; set; }

		/// <summary>
		/// Get the index of the next item that has not yet been materialized in the nominated fill direction. Returns null if there are no more available items in the source.
		/// </summary>
		protected Uno.UI.IndexPath? GetNextUnmaterializedItem(GeneratorDirection fillDirection, Uno.UI.IndexPath? currentMaterializedItem)
		{
			// TODO: adjust for reordering
			return XamlParent?.GetNextItemIndex(currentMaterializedItem, fillDirection == GeneratorDirection.Forward ? 1 : -1);
		}

		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		public static DependencyProperty OrientationProperty { get; } =
			DependencyProperty.Register("Orientation", typeof(Orientation), typeof(ManagedVirtualizingPanelLayout), new FrameworkPropertyMetadata(Orientation.Vertical, (o, e) => ((ManagedVirtualizingPanelLayout)o).OnOrientationChanged((Orientation)e.NewValue)));

		#region CacheLength DependencyProperty

		public double CacheLength
		{
			get { return (double)this.GetValue(CacheLengthProperty); }
			set { this.SetValue(CacheLengthProperty, value); }
		}

		public static DependencyProperty CacheLengthProperty { get; } =
			DependencyProperty.Register(
				"CacheLength",
				typeof(double),
				typeof(ManagedVirtualizingPanelLayout),
				new FrameworkPropertyMetadata(
					defaultValue: (double)4.0,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((ManagedVirtualizingPanelLayout)s)?.OnCacheLengthChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected virtual void OnCacheLengthChanged(double oldCacheLength, double newCacheLength)
		{
			OnCacheLengthChangedPartial(oldCacheLength, newCacheLength);
			OnCacheLengthChangedPartialNative(oldCacheLength, newCacheLength);
		}

		partial void OnCacheLengthChangedPartial(double oldCacheLength, double newCacheLength);
		partial void OnCacheLengthChangedPartialNative(double oldCacheLength, double newCacheLength);

		#endregion
	}
}
#endif
