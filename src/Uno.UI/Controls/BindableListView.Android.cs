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
using Android.Util;
using System.Collections;
using System.Windows.Input;
using Uno.Extensions;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Core;

namespace Uno.UI.Controls
{
	public partial class BindableListView : Android.Widget.ListView, DependencyObject
	{
		private SerialDisposable _clickRegistration = new SerialDisposable();

		public BindableListView(Android.Content.Context context)
			: base(context)
		{
			InitializeBinder();

			Initialize(context, null);
		}

		private void Initialize(Android.Content.Context context, BindableListAdapter adapter)
		{
			if (adapter == null)
			{
				adapter = new BindableListAdapter(context);

				// The default container supports vertical scrolling
				// and stretching for the whole item.
				adapter.ItemContainerTemplate = () => new ListViewItem() { ShouldHandlePressed = false };
			}
			IsResetScrollOnItemsSourceChanged = true;
			Adapter = adapter;
		}

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			SetupItemClickListener();
		}

		protected override void OnDetachedFromWindow()
		{
			base.OnDetachedFromWindow();

			_clickRegistration.Disposable = null;
		}

		public object Header
		{
			get { return BindableAdapter.Header; }
			set { BindableAdapter.Header = value; }
		}

		public object Footer
		{
			get { return BindableAdapter.Footer; }
			set { BindableAdapter.Footer = value; }
		}

		public object HeaderDataContext
		{
			get { return (Header as IDataContextProvider).SelectOrDefault(d => d.DataContext); }
			set
			{
				var provider = Header as IDataContextProvider;
				if (provider != null)
				{
					provider.DataContext = value;
				}
			}
		}

		public object FooterDataContext
		{
			get { return (Footer as IDataContextProvider).SelectOrDefault(d => d.DataContext); }
			set
			{
				var provider = Footer as IDataContextProvider;
				if (provider != null)
				{
					provider.DataContext = value;
				}
			}
		}

		public bool IsResetScrollOnItemsSourceChanged { get; set; }

		public object ItemsSource
		{
			get { return (object)this.GetValue(ItemsSourceProperty); }
			set { this.SetValue(ItemsSourceProperty, value); }
		}

		public static DependencyProperty ItemsSourceProperty { get; } =
			DependencyProperty.Register("ItemsSource", typeof(object), typeof(BindableListView), new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

		private static void OnItemsSourceChanged(object d, DependencyPropertyChangedEventArgs e)
		{
			BindableListView list = d as BindableListView;

			if (list != null)
			{
				if (list.IsResetScrollOnItemsSourceChanged)
				{
					_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () => list.SetSelection(0));
				}
				list.BindableAdapter.ItemsSource = e.NewValue as IEnumerable;
			}
		}

		public int ItemTemplateId
		{
			get { return BindableAdapter.ItemTemplateId; }
			set { BindableAdapter.ItemTemplateId = value; }
		}

		protected BindableListAdapter BindableAdapter
		{
			get { return Adapter as BindableListAdapter; }
		}

		private ICommand _itemClickCommand;
		public ICommand ItemClickCommand
		{
			get
			{
				return _itemClickCommand;
			}
			set
			{
				_itemClickCommand = value;

				SetupItemClickListener();

				if (BindableAdapter != null)
				{
					BindableAdapter.ItemClickCommand = value;
				}
			}
		}

		protected virtual void SetupItemClickListener()
		{
			_clickRegistration.Disposable = null;

			if (ItemClickCommand != null)
			{
				var handler = ExecuteClickCommandEvent(ItemClickCommand);

				base.ItemClick += handler;
				_clickRegistration.Disposable = Disposable.Create(() => base.ItemClick -= handler);
			}
		}

		private EventHandler<ItemClickEventArgs> ExecuteClickCommandEvent(ICommand command)
		{
			return (sender, args) => ExecuteCommandOnItem(command, args.Position);
		}

		private ICommand _itemLongClickCommand;
		public ICommand ItemLongClickCommand
		{
			get
			{
				return _itemLongClickCommand;
			}

			set
			{
				_itemLongClickCommand = value;
				SetupItemLongClickListener();
			}
		}

		protected virtual void SetupItemLongClickListener()
		{
			if (ItemLongClickCommand != null)
			{
				base.ItemLongClick += ExecuteLongClickCommandEvent(ItemLongClickCommand);
			}
			else
			{
				base.ItemLongClick -= ExecuteLongClickCommandEvent(ItemLongClickCommand);
			}
		}

		private EventHandler<ItemLongClickEventArgs> ExecuteLongClickCommandEvent(ICommand command)
		{
			return (sender, args) => ExecuteCommandOnItem(command, args.Position);
		}

		protected void ExecuteCommandOnItem(ICommand command, int position)
		{
			if (command == null)
				return;

			var item = BindableAdapter.GetRawItem(position);
			if (item == null)
				return;

			if (!command.CanExecute(item))
				return;

			command.Execute(item);
		}
	}
}
