using Android.Graphics;
using AndroidX.ViewPager.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Controls
{
	public partial class SlidingTabLayout : HorizontalScrollView
	{
		/**
		 * Allows complete control over the colors drawn in the tab layout. Set with
		 * {@link #setCustomTabColorizer(TabColorizer)}.
		 */
		public interface ITabColorizer
		{

			/**
			 * @return return the color of the indicator used when {@code position} is selected.
			 */
			Color GetIndicatorColor(int position); 

		}

		private const int TitleOffsetDips = 24;
		private const int TabViewPaddingDips = 16;
		private const int TabViewTextSizeSp = 12;

		private int _titleOffset;

		private int _tabViewLayoutId;
		private int _tabViewTextViewId;
		private bool _distributeEvenly;

		private ViewPager _viewPager;
		private SparseArray<string> _contentDescriptions = new SparseArray<string>();
		private ViewPager.IOnPageChangeListener _viewPagerPageChangeListener;

		private readonly SlidingTabStrip _tabStrip;
		private Color _foregroundColor;

		public SlidingTabLayout(Android.Content.Context context)
			: this(context, null)
		{
		}

		public SlidingTabLayout(Android.Content.Context context, IAttributeSet attrs)
			: this(context, attrs, 0)
		{
		}

		public SlidingTabLayout(Android.Content.Context context, IAttributeSet attrs, int defStyle)
			: base(context, attrs, defStyle)
		{

			// Disable the Scroll Bar
			HorizontalScrollBarEnabled = false;
			// Make sure that the Tab Strips fills this View
			FillViewport = true;

			_titleOffset = (int)(TitleOffsetDips * Resources.DisplayMetrics.Density);

			_tabStrip = new SlidingTabStrip(context);
			AddView(_tabStrip, LayoutParams.MatchParent, LayoutParams.WrapContent);
		}

		/**
		 * Set the custom {@link TabColorizer} to be used.
		 *
		 * If you only require simple custmisation then you can use
		 * {@link #setSelectedIndicatorColors(int...)} to achieve
		 * similar effects.
		 */
		public void SetCustomTabColorizer(ITabColorizer tabColorizer)
		{
			_tabStrip.SetCustomTabColorizer(tabColorizer);
		}

		public void SetDistributeEvenly(bool distributeEvenly)
		{
			_distributeEvenly = distributeEvenly;
		}

		public void SetForegroundColor(Color value)
		{
			_foregroundColor = value;
			_tabStrip?.SetSelectedIndicatorColors(_foregroundColor);
			Update();
		}

		/**
		 * Set the {@link ViewPager.OnPageChangeListener}. When using {@link SlidingTabLayout} you are
		 * required to set any {@link ViewPager.OnPageChangeListener} through this method. This is so
		 * that the layout can update it's scroll position correctly.
		 *
		 * @see ViewPager#setOnPageChangeListener(ViewPager.OnPageChangeListener)
		 */
		public void SetOnPageChangeListener(ViewPager.IOnPageChangeListener listener)
		{
			_viewPagerPageChangeListener = listener;
		}

		/**
		 * Set the custom layout to be inflated for the tab views.
		 *
		 * @param layoutResId Layout id to be inflated
		 * @param textViewId id of the {@link TextView} in the inflated view
		 */
		public void SetCustomTabView(int layoutResId, int textViewId)
		{
			_tabViewLayoutId = layoutResId;
			_tabViewTextViewId = textViewId;
		}

		/**
		 * Sets the associated view pager. Note that the assumption here is that the pager content
		 * (number of tabs and tab titles) does not change after this call has been made.
		 */
		public void SetViewPager(ViewPager viewPager)
		{
			_tabStrip.RemoveAllViews();

			_viewPager = viewPager;
			if (viewPager != null)
			{
#pragma warning disable CS0618 // Type or member is obsolete
				viewPager.SetOnPageChangeListener(new InternalViewPagerListener(this));
#pragma warning restore CS0618 // Type or member is obsolete
				PopulateTabStrip();
			}
		}

		/**
		 * Create a default view to be used for tabs. This is called if a custom tab view is not set via
		 * {@link #setCustomTabView(int, int)}.
		 */
		protected TextView CreateDefaultTabView(Android.Content.Context context, int position)
		{
			var textView = new TextView(context);
			textView.Gravity = GravityFlags.Center;
			textView.SetTextSize(ComplexUnitType.Sp, TabViewTextSizeSp);
			textView.Typeface = Typeface.DefaultBold;
			textView.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

			var outValue = new TypedValue();
			Context.Theme.ResolveAttribute(Android.Resource.Attribute.SelectableItemBackground, outValue, true);
			textView.SetBackgroundResource(outValue.ResourceId);
			textView.SetAllCaps(true);

			int padding = (int)(TabViewPaddingDips * Resources.DisplayMetrics.Density);
			textView.SetPadding(padding, padding, padding, padding);
			textView.SetTextColor(_foregroundColor);

			return textView;
		} 

		internal void Update()
		{
			if (_viewPager != null)
			{
				PopulateTabStrip();
			}
		}

		private void PopulateTabStrip()
		{
			var adapter = _viewPager.Adapter;
			var tabClickListener = new TabClickListener(this);
			_tabStrip.RemoveAllViews();

			for (int i = 0; i < adapter.Count; i++)
			{
				View tabView = null;
				TextView tabTitleView = null;

				if (_tabViewLayoutId != 0)
				{
					// If there is a custom tab view layout id set, try and inflate it
					tabView = LayoutInflater.From(Context).Inflate(_tabViewLayoutId, _tabStrip, false);
					tabTitleView = (TextView)tabView.FindViewById(_tabViewTextViewId);
				}

				if (tabView == null)
				{
					tabView = CreateDefaultTabView(Context, i);
				}
				if (tabTitleView == null && tabView is TextView)
				{
					tabTitleView = (TextView)tabView;
				}

				if (_distributeEvenly)
				{
					LinearLayout.LayoutParams lp = (LinearLayout.LayoutParams)tabView.LayoutParameters;
					lp.Width = 0;
					lp.Weight = 1;
				}

				tabTitleView.Text = adapter.GetPageTitle(i);
				tabView.SetOnClickListener(tabClickListener);
				var desc = _contentDescriptions.Get(i, null);
				if (desc != null)
				{
					tabView.ContentDescription = desc;
				}

				_tabStrip.AddView(tabView);

				if (i == _viewPager.CurrentItem)
				{
					tabView.Selected = true;
				}
			}
		}

		public void SetContentDescription(int i, string desc)
		{
			_contentDescriptions.Put(i, desc);
		}

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			if (_viewPager != null)
			{
				ScrollToTab(_viewPager.CurrentItem, 0);
			}
		}

		private void ScrollToTab(int tabIndex, int positionOffset)
		{
			var tabStripChildCount = _tabStrip.ChildCount;
			if (tabStripChildCount == 0 || tabIndex < 0 || tabIndex >= tabStripChildCount)
			{
				return;
			}

			var selectedChild = _tabStrip.GetChildAt(tabIndex);
			if (selectedChild != null)
			{
				int targetScrollX = selectedChild.Left + positionOffset;

				if (tabIndex > 0 || positionOffset > 0)
				{
					// If we're not at the first child and are mid-scroll, make sure we obey the offset
					targetScrollX -= _titleOffset;
				}

				ScrollTo(targetScrollX, 0);
			}
		}

		private class InternalViewPagerListener : Java.Lang.Object, ViewPager.IOnPageChangeListener
		{
			private int _scrollState;

			private SlidingTabLayout _owner;

			public InternalViewPagerListener(SlidingTabLayout owner)
			{
				_owner = owner;
			}

			public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
			{
				var tabStripChildCount = _owner._tabStrip.ChildCount;
				if ((tabStripChildCount == 0) || (position < 0) || (position >= tabStripChildCount))
				{
					return;
				}

				_owner._tabStrip.OnViewPagerPageChanged(position, positionOffset);

				var selectedTitle = _owner._tabStrip.GetChildAt(position);
				int extraOffset = (selectedTitle != null) ? (int)(positionOffset * selectedTitle.Width) : 0;
				_owner.ScrollToTab(position, extraOffset);

				if (_owner._viewPagerPageChangeListener != null)
				{
					_owner._viewPagerPageChangeListener.OnPageScrolled(position, positionOffset, positionOffsetPixels);
				}
			}

			public void OnPageScrollStateChanged(int state)
			{
				_scrollState = state;

				if (_owner._viewPagerPageChangeListener != null)
				{
					_owner._viewPagerPageChangeListener.OnPageScrollStateChanged(state);
				}
			}

			public void OnPageSelected(int position)
			{
				if (_scrollState == ViewPager.ScrollStateIdle)
				{
					_owner._tabStrip.OnViewPagerPageChanged(position, 0f);
					_owner.ScrollToTab(position, 0);
				}
				for (int i = 0; i < _owner._tabStrip.ChildCount; i++)
				{
					_owner._tabStrip.GetChildAt(i).Selected = position == i;
				}
				if (_owner._viewPagerPageChangeListener != null)
				{
					_owner._viewPagerPageChangeListener.OnPageSelected(position);
				}
			}

		}

		private class TabClickListener : Java.Lang.Object, View.IOnClickListener
		{
			private readonly SlidingTabLayout _owner;

			public TabClickListener(SlidingTabLayout owner)
			{
				_owner = owner;
			}

			public void OnClick(View v)
			{
				for (int i = 0; i < _owner._tabStrip.ChildCount; i++)
				{
					if (v == _owner._tabStrip.GetChildAt(i))
					{
						_owner._viewPager.CurrentItem = i;
						return;
					}
				}
			}
		}
	}
}
