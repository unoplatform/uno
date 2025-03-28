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
using Windows.UI.Xaml;

namespace Uno.UI.Controls
{
	public partial class BindableGridView : GridView
	{
		private readonly AbsListViewSecondaryPool _secondaryPool;

		public BindableGridView(Android.Content.Context context, IAttributeSet attrs)
			: this(context, attrs, new BindableListAdapter(context, null))
		{
		}

		public BindableGridView(Android.Content.Context context) : base(context)
		{
			var adapter = new BindableListAdapter(context);
			adapter.CustomViewTypeCount = _customViewTypeCount;
			Adapter = adapter;

			SetupItemClickListeners();

			_secondaryPool = new AbsListViewSecondaryPool(TryGetItemViewTypeFromItem, _customViewTypeCount);
			adapter.SecondaryPool = _secondaryPool;
			SetRecyclerListener(_secondaryPool);
		}

		public const int DefaultCustomViewTypeCount = 50;
		private int _customViewTypeCount = DefaultCustomViewTypeCount;

		/// <summary>
		/// This is used by Android's view recycling. The default value should normally be sufficient. Set it higher if a larger number of unique DataTemplates is needed.
		/// </summary>
		public virtual int CustomViewTypeCount
		{
			get { return _customViewTypeCount; }
			set
			{
				_customViewTypeCount = value;
			}
		}

		public BindableGridView(Android.Content.Context context, IAttributeSet attrs, BindableListAdapter adapter)
			: base(context, attrs)
		{
			Adapter = adapter;
			SetupItemClickListeners();
		}

		public IEnumerable ItemsSource
		{
			get { return (IEnumerable)this.GetValue(ItemsSourceProperty); }
			set { this.SetValue(ItemsSourceProperty, value); }
		}

		public static DependencyProperty ItemsSourceProperty { get; } =
			DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(BindableGridView), new FrameworkPropertyMetadata(null, (s, e) => ((BindableGridView)s)?.InternalOnItemsSourceChanged(e)));

		private void InternalOnItemsSourceChanged(DependencyPropertyChangedEventArgs args)
		{
			if (BindableAdapter != null)
			{
				var newItemsSource = args.NewValue as IEnumerable;
				OnItemsSourceChanging(newItemsSource);
				BindableAdapter.ItemsSource = newItemsSource;
				OnItemsSourceChanged();
			}
		}

		public DataTemplate ItemTemplate
		{
			get { return (DataTemplate)this.GetValue(ItemTemplateProperty); }
			set { this.SetValue(ItemTemplateProperty, value); }
		}

		public static DependencyProperty ItemTemplateProperty { get; } =
			DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(BindableGridView), new FrameworkPropertyMetadata(defaultValue: default(DataTemplate), options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, propertyChangedCallback: (d, s) => (d as BindableGridView)?.OnItemTemplateChanged()));

		private void OnItemTemplateChanged()
		{
			if (BindableAdapter != null)
			{
				BindableAdapter.ItemTemplate = ItemTemplate;
			}
		}

		public int ItemTemplateId
		{
			get { return BindableAdapter.ItemTemplateId; }
			set { BindableAdapter.ItemTemplateId = value; }
		}

		public Windows.UI.Xaml.Controls.DataTemplateSelector ItemTemplateSelector
		{
			get { return BindableAdapter.ItemTemplateSelector; }
			set { BindableAdapter.ItemTemplateSelector = value; }
		}

		protected BindableListAdapter BindableAdapter
		{
			get { return Adapter as BindableListAdapter; }
		}

		public ICommand ItemClickCommand { get; set; }

		public ICommand ItemLongClickCommand { get; set; }

		protected void SetupItemClickListeners()
		{
			base.ItemClick += (sender, args) => ExecuteCommandOnItem(this.ItemClickCommand, args.Position);
			base.ItemLongClick += (sender, args) => ExecuteCommandOnItem(this.ItemLongClickCommand, args.Position);
		}

		private void ExecuteCommandOnItem(ICommand command, int position)
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

		protected virtual void OnItemsSourceChanging(IEnumerable newItems)
		{
		}

		protected virtual void OnItemsSourceChanged()
		{
			this.RequestLayout();
		}

		private int TryGetItemViewTypeFromItem(int position)
		{
			return BindableAdapter?.GetItemViewType(position) ?? 0;
		}

		protected override void RemoveDetachedView(View child, bool animate)
		{
			base.RemoveDetachedView(child, animate);

			_secondaryPool.RemoveFromRecycler(child);
		}
	}
}
