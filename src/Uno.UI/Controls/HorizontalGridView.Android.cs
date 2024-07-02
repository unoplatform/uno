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
using System.Drawing;
using Windows.UI.Core;

namespace Uno.UI.Controls
{
	public partial class HorizontalGridView : BindableHorizontalListView, IOnGridViewItemClickListener
	{
		public HorizontalGridView()
			: base(ContextHelper.Current, null, new GridViewAdapter(ContextHelper.Current))
		{
			BindableAdapter.Background = Background;
			BindableAdapter.Orientation = Windows.UI.Xaml.Controls.Orientation.Horizontal;
		}

		protected override void SetupItemClickListeners()
		{
			BindableAdapter.SetOnItemClickListener(this);
			BindableAdapter.SetOnItemClickListener(this);
		}

		public int RowCount
		{
			get { return BindableAdapter.ColumnOrRowCount; }
			set { BindableAdapter.ColumnOrRowCount = value; }
		}

		public int HorizontalSpacing
		{
			get { return BindableAdapter.HorizontalSpacing; }
			set { BindableAdapter.HorizontalSpacing = value; }
		}

		public int VerticalSpacing
		{
			get { return BindableAdapter.VerticalSpacing; }
			set { BindableAdapter.VerticalSpacing = value; }
		}

		public Func<View> ItemTemplate
		{
			get { return BindableAdapter.ItemTemplate; }
			set { BindableAdapter.ItemTemplate = value; }
		}

		public Func<View> HeaderTemplate
		{
			get { return BindableAdapter.HeaderTemplate; }
			set { BindableAdapter.HeaderTemplate = value; }
		}

		public Func<View> FooterTemplate
		{
			get { return BindableAdapter.FooterTemplate; }
			set { BindableAdapter.FooterTemplate = value; }
		}

		protected new GridViewAdapter BindableAdapter
		{
			get { return base.BindableAdapter as GridViewAdapter; }
		}

		public void OnItemClick(int position, object item, View v)
		{
			ExecuteCommandOnItem(ItemClickCommand, position);
		}

		public void OnItemLongClick(int position, object item, View v)
		{
			ExecuteCommandOnItem(ItemLongClickCommand, position);
		}

		private PointF? _downPoint;
		public override bool OnInterceptTouchEvent(MotionEvent ev)
		{
			if (ev.Action == MotionEventActions.Down)
			{
				_downPoint = new PointF(ev.RawX, ev.RawY);
			}

			if (ev.Action == MotionEventActions.Up && _downPoint.HasValue)
			{
				var deltaX = Math.Abs(_downPoint.Value.X - ev.RawX);
				var deltaY = Math.Abs(_downPoint.Value.Y - ev.RawY);
#pragma warning disable CS0618 // Type or member is obsolete
				var viewConfiguration = new ViewConfiguration();
#pragma warning restore CS0618 // Type or member is obsolete

				_downPoint = null;

				//Ensures the touch is not a scroll, in which case we don't want to propagate it to child views
				return deltaX >= viewConfiguration.ScaledTouchSlop || deltaY >= viewConfiguration.ScaledTouchSlop;
			}

			return base.OnInterceptTouchEvent(ev);
		}
	}
}
