using Android.OS;
using Android.Views;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using AndroidX.Fragment.App;

namespace Windows.UI.Xaml.Controls
{
	public class PivotItemFragment : Fragment
	{
		private readonly PivotItem _item;
		private bool _created = false;

		// Don't delete. Prevents the following exception:
		// System.NotSupportedException: Unable to find the default constructor on type ApplicationFramework.Controls.Tabs.TabFragment.  Please provide the missing constructor.
		public PivotItemFragment()
		{
		}

		public PivotItemFragment(PivotItem item)
		{
			if(item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			_item = item;
		}

		private object _dataContext;
		public object DataContext
		{
			get { return _dataContext; }
			set
			{
				_dataContext = value;
				Update();
			}
		}

		private IFrameworkElement _templatedParent;
		public IFrameworkElement TemplatedParent
		{
			get { return _templatedParent; }
			set
			{
				_templatedParent = value;
				Update();
			}
		}
		
		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			_created = true;
			Update();

			return _item;
		}

		private void Update()
		{
			if (UserVisibleHint && _created && _item != null)
			{
				_item.DataContext = _dataContext;
				_item.TemplatedParent = _templatedParent;
			}
		}

		public override bool UserVisibleHint
		{
			get { return base.UserVisibleHint; }
			set
			{
				base.UserVisibleHint = value;
				Update();
			}
		}
	}
}
