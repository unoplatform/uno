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
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Controls
{
	[Windows.UI.Xaml.Data.Bindable]
	public partial class BindableHorizontalListView : NativeHorizontalListView
	{
		private int _scrollToIndex = -1;
		private bool _scrollToIndexChanged;

		public BindableHorizontalListView(Android.Content.Context context, IAttributeSet attrs)
			: this(context, attrs, new BindableListAdapter(context))
		{

		}

		public BindableHorizontalListView(Android.Content.Context context, IAttributeSet attrs, BindableListAdapter adapter)
			: base(context, attrs)
		{
			Adapter = adapter;
			SetupItemClickListeners();
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayout(changed, left, top, right, bottom);
		}

		protected override void OnDraw(global::Android.Graphics.Canvas canvas)
		{
			base.OnDraw(canvas);
			ScrollToItem();
		}

		public IEnumerable ItemsSource
		{
			get { return BindableAdapter.ItemsSource; }
			set
			{
				BindableAdapter.ItemsSource = value;
			}
		}

		public int ItemTemplateId
		{
			get { return BindableAdapter.ItemTemplateId; }
			set { BindableAdapter.ItemTemplateId = value; }
		}

		public int ScrollToIndex
		{
			get
			{
				return _scrollToIndex;
			}
			set
			{
				_scrollToIndex = value;
				_scrollToIndexChanged = true;
			}
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

				if (BindableAdapter != null)
				{
					BindableAdapter.ItemClickCommand = value;
				}
			}
		}

		public ICommand ItemLongClickCommand { get; set; }

		protected BindableListAdapter BindableAdapter
		{
			get { return Adapter as BindableListAdapter; }
		}

		protected virtual void SetupItemClickListeners()
		{
			base.ItemClick += (sender, args) => ExecuteCommandOnItem(this.ItemClickCommand, args.Position);
			base.ItemLongClick += (sender, args) => ExecuteCommandOnItem(this.ItemLongClickCommand, args.Position);
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

		private void ScrollToItem()
		{
			if (_scrollToIndexChanged
				&& _scrollToIndex >= 0
				&& _scrollToIndex < BindableAdapter.Count)
			{
				_scrollToIndexChanged = false;

				SetSelection(_scrollToIndex);
			}
		}
	}
}
