using Android.App;
using Android.Support.V4.App;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Windows.UI.Xaml.Controls
{
	public class PivotAdapter : FragmentPagerAdapter
	{
		private readonly Pivot _pivot;
		private List<PivotItemFragment> _fragments;

		public PivotAdapter(Android.Support.V4.App.FragmentManager fragmentManager, Pivot pivot)
			: base(fragmentManager)
		{
			if (pivot == null)
			{
				throw new ArgumentNullException(nameof(pivot));
			}

			_pivot = pivot;
			_fragments = new List<PivotItemFragment>();
		}

		public PivotAdapter(Android.Support.V4.App.FragmentManager fm) : base(fm)
		{
		}

		public void OnDataContextChanged()
		{
			foreach (var fragment in _fragments)
			{
				fragment.DataContext = _pivot.DataContext;
				fragment.TemplatedParent = (IFrameworkElement)_pivot.TemplatedParent;
			}
		}

		public override void NotifyDataSetChanged()
		{
			UpdateAdapter();
			base.NotifyDataSetChanged();
		}

		public override Android.Support.V4.App.Fragment GetItem(int position)
		{
			return _fragments[position];
		}

		public override int Count
		{
			get { return _fragments.Count(); }
		}

		public override Java.Lang.ICharSequence GetPageTitleFormatted(int position)
		{
			return new Java.Lang.String((_pivot.Items[position] as PivotItem)?.Header?.ToString() ?? "");
		}

		private void UpdateAdapter()
		{
			_fragments = _pivot
				.SelectOrDefault(p => p.Items)
				.Safe()
				.Select(item => new PivotItemFragment(item as PivotItem)
				{
					DataContext = _pivot.DataContext,
					TemplatedParent = (IFrameworkElement)_pivot.TemplatedParent
				})
				.ToList();
		}
	}
}
