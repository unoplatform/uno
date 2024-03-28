using System;
using System.Linq;
using System.Collections;
using Uno.Disposables;

using Uno.Collections;
using Uno.Extensions;
using Uno.UI.Controls;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Fragment.App;
using Android.Views;
using Android.Widget;
using Android.Util;
using Android.Graphics.Drawables;
using Android.Graphics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using Windows.UI.Xaml;
using Uno.Extensions.Specialized;

namespace Uno.UI.Controls
{
	[Windows.UI.Xaml.Data.Bindable]
	public partial class BindableItemsView : LinearLayout
	{
		CompositeDisposable _subscriptions = new CompositeDisposable();

		IEnumerable _itemsSource;
		HorizontalScrollView _horizontalScrollView;

		bool _scrollIndexChanged = true;

		public BindableItemsView(Android.Content.Context context, IAttributeSet attrs)
			: base(context, attrs)
		{
			_horizontalScrollView = new HorizontalScrollView(context);
			_horizontalScrollView.LayoutParameters = new HorizontalScrollView.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);

			_horizontalScrollView.HorizontalScrollBarEnabled = false;
			this.AddView(_horizontalScrollView);
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			base.OnLayout(changed, l, t, r, b);

			if (_scrollIndexChanged)
			{
				this.ScrollToItem(_scrollToIndex);
				_scrollIndexChanged = false;
			}
		}

		#region Properties
		public IEnumerable ItemsSource
		{
			get { return _itemsSource; }
			set
			{
				if (_itemsSource != value)
				{
					_itemsSource = value;
					UpdateSource(value);
				}
			}
		}

		public int ItemsTemplateId { get; set; }
		public ICommand ItemClickCommand { get; set; }
		public ICommand ItemLongClickCommand { get; set; }
		#endregion

		public event EventHandler<ItemsSourceChangedEventArgs> ItemsSourceChanged
		{
			add
			{
				_itemsSourceChanged += value;
			}
			remove
			{
				_itemsSourceChanged -= value;
			}
		}
		private EventHandler<ItemsSourceChangedEventArgs> _itemsSourceChanged;

		int _scrollToIndex = -1;
		public int ScrollToIndex
		{
			get
			{
				return _scrollToIndex;
			}
			set
			{
				_scrollToIndex = value;

				_scrollIndexChanged = true;
			}
		}


		public void ScrollToItem(int itemIndex)
		{
			var index = itemIndex;
			if (itemIndex < 0)
			{
				index = 0;
			}

			if (itemIndex > _itemsSource.Count())
			{
				index = _itemsSource.Count() - 1;
			}

			LinearLayout linearLayout = (LinearLayout)_horizontalScrollView.GetChildAt(0);
			if (linearLayout != null)
			{
				View itemAtIndex = linearLayout.GetChildAt(index);
				if (itemAtIndex != null)
				{
					_horizontalScrollView.ScrollTo(itemAtIndex.Left, 0);
				}
			}
		}

		private void UpdateSource(IEnumerable itemsSource)
		{
			CreateChildren(itemsSource);

			if (_itemsSourceChanged != null)
			{
				_itemsSourceChanged(this, new ItemsSourceChangedEventArgs(_itemsSource));
			}
		}

		private void CreateChildren(IEnumerable itemsSource)
		{
			if (itemsSource == null)
			{
				return;
			}

			var activity = this.Context as FragmentActivity;

			if (activity != null)
			{
				_horizontalScrollView.RemoveAllViews();
				LinearLayout scrollableLinearLayout = new LinearLayout(this.Context);
				scrollableLinearLayout.LayoutParameters = new LinearLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);

				_horizontalScrollView.AddView(scrollableLinearLayout);

				foreach (object obj in itemsSource)
				{
					var view = CreateView(activity, ItemsTemplateId, obj)
						.InitializeClick(this.Clickable, _subscriptions, DoClickAction)
						.InitializeLongClick(this.LongClickable, _subscriptions, DoLongClickAction)
						;
					scrollableLinearLayout.AddView(view);
				}
			}
		}

		private View CreateView(Android.Content.Context context, int templateId, object source)
		{
			BindableView view = null;

			if (templateId != 0)
			{
				view = new BindableView(context, templateId);
				view.DataContext = source;
			}

			return view;
		}

		#region DoExecuteCommand		

		private void DoClickAction(object sender, EventArgs obj)
		{
			var view = sender as View;

			if (view != null)
			{
				DoExecuteCommand(view, ItemClickCommand);
			}
		}

		private void DoLongClickAction(object sender, LongClickEventArgs obj)
		{
			var view = sender as View;

			if (view != null)
			{
				DoExecuteCommand(view, ItemLongClickCommand);
			}
		}

		private void DoExecuteCommand(View view, ICommand command)
		{
			if (command == null)
			{
				return;
			}

			var dataProvider = view as IDataContextProvider;

			if (dataProvider == null)
			{
				return;
			}

			if (!command.CanExecute(dataProvider.DataContext))
			{
				return;
			}

			command.Execute(dataProvider.DataContext ?? default(object));
		}

	}
	#endregion

	public class ItemsSourceChangedEventArgs : EventArgs
	{
		public ItemsSourceChangedEventArgs(IEnumerable source)
		{
			ItemsSource = source;
		}
		public IEnumerable ItemsSource { get; set; }
	}
}
