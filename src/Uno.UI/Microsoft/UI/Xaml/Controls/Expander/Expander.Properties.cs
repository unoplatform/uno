using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Expander
	{
		public ExpandDirection ExpandDirection
		{
			get => (ExpandDirection)GetValue(ExpandDirectionProperty);
			set => SetValue(ExpandDirectionProperty, value);
		}

		public static DependencyProperty ExpandDirectionProperty { get; } =
			DependencyProperty.Register(
				nameof(ExpandDirection),
				typeof(ExpandDirection),
				typeof(Expander),
				new PropertyMetadata(
					ExpandDirection.Down,
					(s, e) => (s as Expander)?.OnExpandDirectionPropertyChanged(e)));

		public object Header
		{
			get => GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				nameof(Header),
				typeof(object),
				typeof(Expander),
				new PropertyMetadata(null));

		public DataTemplate HeaderTemplate
		{
			get => GetValue(HeaderTemplateProperty) as DataTemplate;
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(HeaderTemplate),
				typeof(DataTemplate),
				typeof(Expander),
				new PropertyMetadata(null));

		public DataTemplateSelector HeaderTemplateSelector
		{
			get => GetValue(HeaderTemplateSelectorProperty) as DataTemplateSelector;
			set => SetValue(HeaderTemplateSelectorProperty, value);
		}

		public static DependencyProperty HeaderTemplateSelectorProperty { get; } =
			DependencyProperty.Register(
				nameof(HeaderTemplateSelector),
				typeof(DataTemplateSelector),
				typeof(Expander),
				new PropertyMetadata(null));

		public bool IsExpanded
		{
			get => (bool)GetValue(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}

		public static DependencyProperty IsExpandedProperty { get; } =
			DependencyProperty.Register(
				nameof(IsExpanded),
				typeof(bool),
				typeof(Expander),
				new PropertyMetadata(
					true,
					(s, e) => (s as Expander)?.OnIsExpandedPropertyChanged(e)));

		public event TypedEventHandler<Expander, ExpanderExpandingEventArgs> Expanding;
		public event TypedEventHandler<Expander, ExpanderCollapsedEventArgs> Collapsed;
	}
}
