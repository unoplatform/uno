using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;

namespace Uno.UI.Extensions
{
	public static class FragmentManagerExtensions
	{
		public static BaseFragment GetCurrentFragment(this FragmentManager fragmentManager)
		{
			if (fragmentManager.BackStackEntryCount > 0)
			{
				var entry = fragmentManager.GetBackStackEntryAt(fragmentManager.BackStackEntryCount - 1);

				var fragment = fragmentManager.FindFragmentByTag(entry.Name) as BaseFragment;

				return fragment;
			}

			return null;
		}
	}
}
