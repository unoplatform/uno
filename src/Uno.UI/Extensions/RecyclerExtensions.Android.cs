using System;
using System.Collections.Generic;
using System.Text;
using AndroidX.RecyclerView.Widget;
using Android.Views;

namespace Uno.UI.Extensions
{
	public static class RecyclerExtensions
	{
		/// <summary>
		/// Wrapper of <see cref="RecyclerView.Recycler.GetViewForPosition(int)"/> that duplicates bounds checking in managed code, for easier error handling.
		/// </summary>
		public static View GetViewForPosition(this RecyclerView.Recycler recycler, int position, RecyclerView.State state)
		{
			if (position < 0 || position >= state.ItemCount)
			{
				throw new IndexOutOfRangeException($"Invalid item position ({position}). Item count:{state.ItemCount}");
			}

			return recycler.GetViewForPosition(position);
		}
	}
}
