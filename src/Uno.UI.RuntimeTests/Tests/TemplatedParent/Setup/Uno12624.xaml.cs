using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;

public sealed partial class Uno12624 : Page
{
	public Uno12624()
	{
		this.InitializeComponent();
	}
}

public partial class Uno12624_LeftRightControl : Control
{
	#region DependencyProperty: Left

	public static DependencyProperty LeftProperty { get; } = DependencyProperty.Register(
		nameof(Left),
		typeof(object),
		typeof(Uno12624_LeftRightControl),
		new PropertyMetadata(default(object)));

#if !__ANDROID__
	public object Left
#else
	public new object Left
#endif
	{
		get => (object)GetValue(LeftProperty);
		set => SetValue(LeftProperty, value);
	}

	#endregion
	#region DependencyProperty: Right

	public static DependencyProperty RightProperty { get; } = DependencyProperty.Register(
		nameof(Right),
		typeof(object),
		typeof(Uno12624_LeftRightControl),
		new PropertyMetadata(default(object)));

#if !__ANDROID__
	public object Right
#else
	public new object Right
#endif
	{
		get => (object)GetValue(RightProperty);
		set => SetValue(RightProperty, value);
	}

	#endregion
}

public partial class Uno12624_WestEastControl : Control
{
	#region DependencyProperty: West

	public static DependencyProperty WestProperty { get; } = DependencyProperty.Register(
		nameof(West),
		typeof(object),
		typeof(Uno12624_WestEastControl),
		new PropertyMetadata(default(object)));

	public object West
	{
		get => (object)GetValue(WestProperty);
		set => SetValue(WestProperty, value);
	}

	#endregion
	#region DependencyProperty: East

	public static DependencyProperty EastProperty { get; } = DependencyProperty.Register(
		nameof(East),
		typeof(object),
		typeof(Uno12624_WestEastControl),
		new PropertyMetadata(default(object)));

	public object East
	{
		get => (object)GetValue(EastProperty);
		set => SetValue(EastProperty, value);
	}

	#endregion
}
