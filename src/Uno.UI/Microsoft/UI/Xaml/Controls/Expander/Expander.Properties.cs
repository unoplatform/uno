using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
				new FrameworkPropertyMetadata(
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
				new FrameworkPropertyMetadata(null));

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
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

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
				new FrameworkPropertyMetadata(null));

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
				new FrameworkPropertyMetadata(
					true,
					(s, e) => (s as Expander)?.OnIsExpandedPropertyChanged(e)));

		public ExpanderTemplateSettings TemplateSettings
		{
			get => (ExpanderTemplateSettings)GetValue(TemplateSettingsProperty);
			set => SetValue(TemplateSettingsProperty, value);
		}

		public static DependencyProperty TemplateSettingsProperty { get; } =
			DependencyProperty.Register(
				nameof(TemplateSettings),
				typeof(ExpanderTemplateSettings),
				typeof(Expander),
				new FrameworkPropertyMetadata(null));

		public event TypedEventHandler<Expander, ExpanderExpandingEventArgs> Expanding;
		public event TypedEventHandler<Expander, ExpanderCollapsedEventArgs> Collapsed;
	}
}
