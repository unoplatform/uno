using System;
using Android.Views;
using Android.Widget;
using Windows.UI.Xaml.Controls;
using Android.Runtime;
using Uno.Extensions;

namespace Uno.UI.Controls
{
    public abstract class ItemContainerHolderAdapter : BaseAdapter
    {
        public Windows.UI.Xaml.Controls.Orientation? ItemContainerHolderStretchOrientation { get; set; }

		/// <summary>
		/// An optional secondary view pool that can handle control reloads.
		/// </summary>
		public ISecondaryViewPool SecondaryPool { get; set; }

		public override View GetView(int position, View convertView, ViewGroup parent)
        {
			try
			{
				var wrapper = (convertView ?? SecondaryPool?.GetView(position)) as ItemContainerHolder;
				if (wrapper == null)
				{
					wrapper = new ItemContainerHolder
					{
						StretchOrientation = ItemContainerHolderStretchOrientation
					};
				}

				wrapper.Child = GetContainerView(position, wrapper.Child, parent);

				var viewGroup = wrapper as ViewGroup;
				if (viewGroup != null)
				{
					// This is here to avoid disabling the ItemClick event when an Item Template has a button 
					// as any of its children.
					viewGroup.DescendantFocusability = Android.Views.DescendantFocusability.BlockDescendants;
				}

				SecondaryPool?.SetActiveView(position, wrapper);

				//We set the wrapper LayoutParameters because AbsListView needs its child's LayoutParams to be of type AbsListView.LayoutParams.
				//If LayoutParams are not set to the specific type they cannot be casted to AbsListView.LayoutParams.
				//This is required for Android 4.4 specifically and Spinner.
				if (parent is Spinner)
				{
					wrapper.LayoutParameters = new AbsListView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
				}

				return wrapper;
			}
			catch(Exception e)
			{
				Windows.UI.Xaml.Application.Current.RaiseRecoverableUnhandledExceptionOrLog(e, this);

				return new Windows.UI.Xaml.FrameworkElement();
			}
        }

        protected abstract View GetContainerView(int position, View convertView, ViewGroup parent);
    }
}