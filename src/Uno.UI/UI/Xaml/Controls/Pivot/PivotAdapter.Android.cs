using Android.App;
using AndroidX.Fragment.App;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Windows.UI.Xaml.Controls
{
#pragma warning disable CS0618 // Type or member is obsolete
	public class PivotAdapter : FragmentPagerAdapter
#pragma warning restore CS0618 // Type or member is obsolete
	{
		private readonly NativePivotPresenter _pivot;
		private List<PivotItemFragment> _fragments;

#pragma warning disable CS0618 // Type or member is obsolete
		public PivotAdapter(AndroidX.Fragment.App.FragmentManager fragmentManager, NativePivotPresenter pivot)
			: base(fragmentManager)
		{
			if (pivot == null)
			{
				throw new ArgumentNullException(nameof(pivot));
			}

			_pivot = pivot;
			_fragments = new List<PivotItemFragment>();
		}

		public PivotAdapter(AndroidX.Fragment.App.FragmentManager fm) : base(fm)
		{
		}
#pragma warning restore CS0618 // Type or member is obsolete

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

		public override AndroidX.Fragment.App.Fragment GetItem(int position)
		{
			return _fragments[position];
		}

		public override int Count
		{
			get { return _fragments.Count; }
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
				.Select(item =>
				{
					var pivotItem = item as PivotItem;

					if (
						pivotItem.Parent != null
						&& pivotItem.Parent.GetType() != typeof(PivotItemFragment)
						)
					{
						// Assume items have been added to the UWP default items container
						// Remove it until a cleanup can be done properly in style management.
						(pivotItem.Parent as Panel).Children.Remove(pivotItem);
					}

					var fragment = new PivotItemFragment(pivotItem)
					{
						DataContext = _pivot.DataContext,
						TemplatedParent = (IFrameworkElement)_pivot.TemplatedParent
					};
					return fragment;
				})
				.ToList();
		}
	}
}
