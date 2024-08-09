using System.Collections.Generic;

namespace Windows.UI.Xaml.Controls.Primitives
{
	partial class LoopingSelector
	{
		public event SelectionChangedEventHandler SelectionChanged;

		public bool ShouldLoop
		{
			get => (bool)this.GetValue(ShouldLoopProperty);
			set => this.SetValue(ShouldLoopProperty, value);
		}


		public object SelectedItem
		{
			get => this.GetValue(SelectedItemProperty);
			set => this.SetValue(SelectedItemProperty, value);
		}


		public int SelectedIndex
		{
			get => (int)this.GetValue(SelectedIndexProperty);
			set => this.SetValue(SelectedIndexProperty, value);
		}


		public IList<object> Items
		{
			get => (IList<object>)this.GetValue(ItemsProperty);
			set => this.SetValue(ItemsProperty, value);
		}


		public int ItemWidth
		{
			get => (int)this.GetValue(ItemWidthProperty);
			set => this.SetValue(ItemWidthProperty, value);
		}


		public DataTemplate ItemTemplate
		{
			get => (DataTemplate)this.GetValue(ItemTemplateProperty);
			set => this.SetValue(ItemTemplateProperty, value);
		}


		public int ItemHeight
		{
			get => (int)this.GetValue(ItemHeightProperty);
			set => this.SetValue(ItemHeightProperty, value);
		}


		public static DependencyProperty ItemHeightProperty { get; } =
			DependencyProperty.Register(
				nameof(ItemHeight), typeof(int),
				typeof(Primitives.LoopingSelector),
				new FrameworkPropertyMetadata(default(int)));


		public static DependencyProperty ItemTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(ItemTemplate), typeof(DataTemplate),
				typeof(Primitives.LoopingSelector),
				new FrameworkPropertyMetadata(default(DataTemplate), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));


		public static DependencyProperty ItemWidthProperty { get; } =
			DependencyProperty.Register(
				nameof(ItemWidth), typeof(int),
				typeof(Primitives.LoopingSelector),
				new FrameworkPropertyMetadata(default(int)));


		public static DependencyProperty ItemsProperty { get; } =
			DependencyProperty.Register(
				nameof(Items), typeof(IList<object>),
				typeof(Primitives.LoopingSelector),
				new FrameworkPropertyMetadata(default(IList<object>)));


		public static DependencyProperty SelectedIndexProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectedIndex), typeof(int),
				typeof(Primitives.LoopingSelector),
				new FrameworkPropertyMetadata(default(int)));


		public static DependencyProperty SelectedItemProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectedItem), typeof(object),
				typeof(Primitives.LoopingSelector),
				new FrameworkPropertyMetadata(default(object)));


		public static DependencyProperty ShouldLoopProperty { get; } =
			DependencyProperty.Register(
				nameof(ShouldLoop), typeof(bool),
				typeof(Primitives.LoopingSelector),
				new FrameworkPropertyMetadata(default(bool)));
	}
}
