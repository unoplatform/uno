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

using Uno.Extensions;
using System.Collections;
using Android.Graphics.Drawables;
using Android.Graphics;
using System.Globalization;
using Uno.Foundation.Logging;
using Windows.UI.Xaml;
using Uno.Extensions.Specialized;

namespace Uno.UI.Controls
{
	public class GridViewAdapter : BindableListAdapter
	{
		private IOnGridViewItemClickListener _itemClickListener;
		private Color _dividerColor;

		//private readonly List<View> _createdRows = new List<View>();

		public GridViewAdapter(Android.Content.Context context)
			: base(context)
		{
			_columnOrRowCount = 1;
			Orientation = Windows.UI.Xaml.Controls.Orientation.Vertical;
		}

		private int _columnOrRowCount;

		public int ColumnOrRowCount
		{
			get { return _columnOrRowCount; }
			set
			{
				_columnOrRowCount = value;
				NotifyDataSetChanged();
			}
		}

		public Windows.UI.Xaml.Controls.Orientation Orientation { get; set; }

		public int VerticalSpacing { get; set; }
		public int HorizontalSpacing { get; set; }
		public Drawable Background { get; set; }


		public Color DividerColor
		{
			get { return _dividerColor; }
			set { _dividerColor = value; }
		}

		public override int Count
		{
			get
			{
				var list = ItemsSource as IList;

				var count = list == null ? ItemsSource.Count() : list.Count;

				var rowCount = (int)Math.Ceiling(count * 1f / ColumnOrRowCount);

				if (HasHeader())
				{
					rowCount += 1;
				}

				if (HasFooter())
				{
					rowCount += 1;
				}

				return rowCount;
			}
		}

		public void SetOnItemClickListener(IOnGridViewItemClickListener listener)
		{
			_itemClickListener = listener;
		}

		protected override View GetItemView(int position, View convertView, ViewGroup parent, int templateId)
		{
			var convertLayout = convertView as LinearLayout;

			var rowView = convertLayout == null
				? CreateRow(position, templateId)
				: UpdateRow(position, templateId, convertLayout);

			switch (Orientation)
			{
				case Windows.UI.Xaml.Controls.Orientation.Vertical:
					rowView.SetPadding(0, position == 0 ? 0 : VerticalSpacing, 0, 0);
					break;
				case Windows.UI.Xaml.Controls.Orientation.Horizontal:
					rowView.SetPadding(position == 0 ? 0 : HorizontalSpacing, 0, 0, 0);
					break;
			}

			return rowView;
		}

		private View CreateRow(int rowIndex, int templateId)
		{
			var rowLayout = new LinearLayout(Context)
			{
				Orientation = Orientation == Windows.UI.Xaml.Controls.Orientation.Horizontal
					? Android.Widget.Orientation.Vertical
					: Android.Widget.Orientation.Horizontal,
				WeightSum = ColumnOrRowCount,
				LayoutParameters = new AbsListView.LayoutParams(AbsListView.LayoutParams.MatchParent, AbsListView.LayoutParams.MatchParent)
			};

			rowLayout.SetBackgroundColor(_dividerColor);

			var rowItems = GetRowItemViews(
				rowIndex,
				templateId
			);

			rowItems.ForEach(v => rowLayout.AddView(v));

			//_createdRows.Add(rowLayout);

			return rowLayout;
		}

		private View UpdateRow(int rowIndex, int templateId, LinearLayout rowLayout)
		{
			var rowItems = GetRowItemViews(
				rowIndex,
				templateId,
				columnIndex => rowLayout.GetChildAt(columnIndex)
			);

			rowLayout.RemoveAllViews();

			rowItems.ForEach(v => rowLayout.AddView(v));

			return rowLayout;
		}

		private View[] GetRowItemViews(int rowIndex, int templateId, Func<int, View> convertViewFactory = null)
		{
			return Enumerable
				.Range(0, ColumnOrRowCount)
				.Select(i => new { ConvertView = convertViewFactory.SelectOrDefault(f => f(i)), Position = AdjustPosition(rowIndex * ColumnOrRowCount + i), IsLast = i == ColumnOrRowCount - 1 })
				.Select(a => new { View = GetItemView(a.Position, a.ConvertView, templateId), a.IsLast })
				.Where(a => a.View != null)
				.Do(a => SetItemLayout(a.View, a.IsLast))
				.Select(a => a.View)
				.ToArray();
		}

		private View GetItemView(int position, View convertView, int templateId)
		{
			if (AdjustPosition(position) >= ItemsSource.Count())
			{
				return null;
			}

			if (templateId == 0)
			{
				var view = convertView;
				var source = GetRawItem(position);

				if (view == null)
				{
					var template = Windows.UI.Xaml.DataTemplateHelper.ResolveTemplate(
						this.ItemTemplate,
						this.ItemTemplateSelector,
						source,
						null
					);

					view = template?.LoadContentCached();
					view.Click += ItemViewClicked;
					view.LongClick += ItemViewLongClicked;
				}

				view.Tag = position.ToStringInvariant();
				var bindable = view as IDataContextProvider;

				if (bindable != null)
				{
					bindable.DataContext = GetRawItem(position);
				}

				return view;
			}
			else
			{
				var bindableView = convertView as BindableView;

				if (bindableView.SelectOrDefault(v => v.LayoutId != templateId, true))
				{
					bindableView = new BindableView(Context, templateId);
					bindableView.Click += ItemViewClicked;
					bindableView.LongClick += ItemViewLongClicked;
				}

				bindableView.Tag = position.ToString(CultureInfo.InvariantCulture);
				bindableView.DataContext = GetRawItem(position);

				return bindableView;
			}
		}

		private void ItemViewClicked(object sender, EventArgs args)
		{
			var view = sender as View;

			if (view != null)
			{
				var position = int.Parse(view.Tag.ToString(), CultureInfo.InvariantCulture);
				_itemClickListener?.OnItemClick(position, GetRawItem(position), view);
			}
		}

		private void ItemViewLongClicked(object sender, EventArgs args)
		{
			var view = sender as View;

			if (view != null)
			{
				var position = int.Parse(view.Tag.ToString(), CultureInfo.InvariantCulture);
				_itemClickListener?.OnItemLongClick(position, GetRawItem(position), view);
			}
		}

		private void SetItemLayout(View view, bool isLastColumnOrRow)
		{
			if (view != null)
			{
				LinearLayout.LayoutParams layoutParameters;

				switch (Orientation)
				{
					case Windows.UI.Xaml.Controls.Orientation.Vertical:
						layoutParameters = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.MatchParent, 1);
						layoutParameters.SetMargins(0, 0, isLastColumnOrRow ? 0 : VerticalSpacing, 0);
						break;
					case Windows.UI.Xaml.Controls.Orientation.Horizontal:
						layoutParameters = new LinearLayout.LayoutParams(0, 1, LinearLayout.LayoutParams.MatchParent);
						layoutParameters.SetMargins(0, 0, 0, isLastColumnOrRow ? 0 : HorizontalSpacing);
						break;
					default:
						layoutParameters = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
						break;
				}

				view.LayoutParameters = layoutParameters;
			}
		}
	}
}
