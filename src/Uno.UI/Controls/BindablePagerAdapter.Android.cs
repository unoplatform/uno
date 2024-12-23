using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.ViewPager.Widget;
using Uno.Disposables;
using System.Collections;
using System.Collections.Specialized;
using Uno.Extensions;
using System.Windows.Input;
using Uno.Foundation.Logging;
using Object = Java.Lang.Object;
using Uno.Extensions.Specialized;

namespace Uno.UI.Controls
{
	[Windows.UI.Xaml.Data.Bindable]
	public class BindablePagerAdapter : PagerAdapter, View.IOnClickListener
	{
		private IEnumerable _itemsSource;
		private Dictionary<int, View> _views = new Dictionary<int, View>();

		public BindablePagerAdapter()
		{
		}

		public override int Count
		{
			get
			{
				var list = _itemsSource as IList;

				if (list != null)
				{
					return list.Count;
				}
				else if (_itemsSource != null)
				{
					return _itemsSource.Count();
				}
				else
				{
					return 0;
				}
			}
		}

		public System.Object GetRawItem(int position)
		{
			return _itemsSource.ElementAt(position);
		}

		public override Java.Lang.Object InstantiateItem(ViewGroup container, int position)
		{
			var convertView = _views.UnoGetValueOrDefault(position);
			return GetView(position, convertView, container, ItemTemplateId);
		}

#pragma warning disable CS0672 // Member overrides obsolete member
		public override void DestroyItem(View container, int position, Java.Lang.Object @object)
#pragma warning restore CS0672 // Member overrides obsolete member
		{
			((ViewGroup)container).RemoveView((View)@object);
		}

		public override void DestroyItem(ViewGroup container, int position, Java.Lang.Object @object)
		{
			container.RemoveView((View)@object);
		}

		private View GetView(int position, View convertView, ViewGroup parent, int templateId)
		{
			if (_itemsSource == null)
			{
				this.Log().Debug("GetView called when ItemsSource is null");
				return null;
			}

			var source = GetRawItem(position);

			var view = GetBindableView(convertView, source, templateId, parent);

			_views.TryAdd(position, view);

			view.SetOnClickListener(this);

			parent.AddView(view);

			return view;
		}


		protected virtual View GetBindableView(View convertView, object source, ViewGroup parent)
		{
			return GetBindableView(convertView, source, ItemTemplateId, parent);
		}

		protected virtual View GetBindableView(View convertView, object source, int templateId, ViewGroup parent)
		{
			if (templateId == 0)
			{
				// no template seen - so use a standard string view from Android and use ToString()
				return GetSimpleView(convertView, source);
			}

			var bindableView = convertView as BindableView;

			if (convertView == null)
			{
				bindableView = new BindableView(parent.Context, templateId);
			}

			bindableView.DataContext = source;

			return bindableView;
		}

		private View GetSimpleView(View convertView, object source)
		{
			return null;
		}

		public virtual int ItemTemplateId { get; set; }

		public virtual System.Collections.IEnumerable ItemsSource
		{
			get { return _itemsSource; }
			set
			{

				if (_itemsSource != value)
				{
					_itemsSource = value;

					UpdateList();
				}
			}
		}

		protected virtual void UpdateList()
		{
			NotifyDataSetChanged();
		}

		public override bool IsViewFromObject(View view, Java.Lang.Object @object)
		{
			return view == @object;
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				base.Dispose(disposing);
				ClearViews();
			}
			catch (Exception e)
			{
				this.Log().ErrorFormat("Failed to dispose view", e);
			}
		}

		private void ClearViews()
		{
			_views.Values.ForEach((View v) => v.TryDispose());
			_views.Clear();
		}

		internal void ClearRecycledViews()
		{
			_views.Clear();
		}

		public virtual ICommand ItemClickCommand { get; set; }

		public void OnClick(View v)
		{
			var bv = v as BindableView;
			if (bv != null && ItemClickCommand != null)
			{
				ItemClickCommand.Execute(bv.DataContext);
			}
		}
	}
}
