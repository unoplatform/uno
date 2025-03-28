using Android.App;
using Android.Graphics;
using AndroidX.ViewPager.Widget;
using AndroidX.Fragment.App;
using Android.Util;
using Android.Views;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativePivotPresenter
	{
		private SlidingTabLayout _tabStrip;
		private ViewPager _pager;

		private PivotAdapter _adapter;

		partial void InitializePartial()
		{
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_pager = this.FindFirstChild<ViewPager>();
			_tabStrip = this.FindFirstChild<SlidingTabLayout>();

			UpdateForeground();

			ResetAdapter();
			UpdateItems();
		}

		protected override void OnForegroundColorChanged(Brush oldValue, Brush newValue)
		{
			base.OnForegroundColorChanged(oldValue, newValue);

			UpdateForeground();
		}

		private void UpdateForeground()
		{
			var newColor = Foreground as SolidColorBrush;
			if (newColor != null)
			{
				_tabStrip?.SetForegroundColor(newColor.Color);
			}
		}

		private void ResetAdapter()
		{
			_pager.Adapter = null;

			var oldAdapter = _adapter;
			_adapter = new PivotAdapter((Context as FragmentActivity).SupportFragmentManager, this);

			// have to dispose it after we've set the view pager, otherwise an error occurs because we've dumped out
			// the Java Reference.
			if (oldAdapter != null)
			{
				oldAdapter.Dispose();
			}

			_pager.Adapter = _adapter;
			_tabStrip?.SetViewPager(_pager);
		}

		protected internal override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			_adapter?.OnDataContextChanged();
		}

		partial void UpdateItems()
		{
			_adapter?.NotifyDataSetChanged();
			if (_pager != null)
			{
				_pager.OffscreenPageLimit = Items.Count;
			}
			_tabStrip?.Update();
		}
	}
}
