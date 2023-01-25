using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.ComponentModel;

namespace Uno.UI.Controls
{
#if __ANDROID__ || __IOS__
	/// <summary>
	/// An ItemsStackPanel implementation which doesn't rely on high-level native list controls.
	/// </summary>
	/// <remarks>For now this panel mainly exists for testing purposes, to be able to debug the WASM/MacOS implementation on Android or iOS.</remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public partial class ManagedItemsStackPanel : Panel
	{
		ManagedVirtualizingPanelLayout _layout;

#if !__IOS__
		internal bool ShouldInterceptInvalidate { get; set; }
#endif

		public ManagedItemsStackPanel()
		{
			if (FeatureConfiguration.ListViewBase.DefaultCacheLength.HasValue)
			{
				CacheLength = FeatureConfiguration.ListViewBase.DefaultCacheLength.Value;
			}

			CreateLayoutIfNeeded();
			_layout.Initialize(this);
		}

		internal ManagedVirtualizingPanelLayout GetLayouter()
		{
			CreateLayoutIfNeeded();
			return _layout;
		}

		private void CreateLayoutIfNeeded()
		{
			if (_layout == null)
			{
				_layout = new ManagedItemsStackPanelLayout();
				_layout.BindToEquivalentProperty(this, nameof(Orientation));
				//				_layout.BindToEquivalentProperty(this, nameof(AreStickyGroupHeadersEnabled));
				//				_layout.BindToEquivalentProperty(this, nameof(GroupHeaderPlacement));
				//				_layout.BindToEquivalentProperty(this, nameof(GroupPadding));

				_layout.BindToEquivalentProperty(this, nameof(CacheLength));
			}
		}

		#region Orientation DependencyProperty

		public Orientation Orientation
		{
			get { return (Orientation)this.GetValue(OrientationProperty); }
			set { this.SetValue(OrientationProperty, value); }
		}

		public static DependencyProperty OrientationProperty { get; } =
			DependencyProperty.Register(
				"Orientation",
				typeof(Orientation),
				typeof(ManagedItemsStackPanel),
				new FrameworkPropertyMetadata(
					defaultValue: (Orientation)Orientation.Vertical,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((ManagedItemsStackPanel)s)?.OnOrientationChanged((Orientation)e.OldValue, (Orientation)e.NewValue)
				)
			);

		protected virtual void OnOrientationChanged(Orientation oldOrientation, Orientation newOrientation)
		{
			OnOrientationChangedPartial(oldOrientation, newOrientation);
			OnOrientationChangedPartialNative(oldOrientation, newOrientation);
		}

		partial void OnOrientationChangedPartial(Orientation oldOrientation, Orientation newOrientation);
		partial void OnOrientationChangedPartialNative(Orientation oldOrientation, Orientation newOrientation);

		#endregion

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
				typeof(ManagedItemsStackPanel),
				new FrameworkPropertyMetadata(
					defaultValue: (double)4.0,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((ManagedItemsStackPanel)s)?.OnCacheLengthChanged((double)e.OldValue, (double)e.NewValue)
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

#if __IOS__
		public override void SetSuperviewNeedsLayout()
		{
			if (ShouldInterceptInvalidate)
			{
				return;
			}

			base.SetSuperviewNeedsLayout();
		}
#elif __ANDROID__
		protected override bool NativeRequestLayout()
		{
			if (ShouldInterceptInvalidate)
			{
				ForceLayout();
				return false;
			}
			else
			{
				return base.NativeRequestLayout();
			}
		}
#endif
	}
#else
	public partial class ManagedItemsStackPanel : ItemsStackPanel { } // Make available on other platforms for Xaml compatibility
#endif
}
