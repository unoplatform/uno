using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;

public sealed partial class Uno17313 : Page
{
	public Uno17313()
	{
		this.InitializeComponent();
	}
}

public partial class Uno17313_HeaderedCC : ContentControl
{
	#region DependencyProperty: Header

	public static DependencyProperty HeaderProperty { get; } = DependencyProperty.Register(
		nameof(Header),
		typeof(object),
		typeof(Uno17313_HeaderedCC),
		new PropertyMetadata(default(object)));

	public object Header
	{
		get => (object)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	#endregion
	#region DependencyProperty: HeaderTemplate

	public static DependencyProperty HeaderTemplateProperty { get; } = DependencyProperty.Register(
		nameof(HeaderTemplate),
		typeof(DataTemplate),
		typeof(Uno17313_HeaderedCC),
		new PropertyMetadata(default(DataTemplate)));

	public DataTemplate HeaderTemplate
	{
		get => (DataTemplate)GetValue(HeaderTemplateProperty);
		set => SetValue(HeaderTemplateProperty, value);
	}

	#endregion
}

public partial class Uno17313_Expander : Uno17313_HeaderedCC { }
public partial class Uno17313_SettingsExpander : Uno17313_HeaderedCC { }
public partial class Uno17313_SettingsCard : Uno17313_HeaderedCC { }
