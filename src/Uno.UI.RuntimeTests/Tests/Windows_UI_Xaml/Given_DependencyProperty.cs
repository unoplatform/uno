using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public partial class Given_DependencyProperty
{
	private partial class MyButton : Button
	{
		private static int _value;

		public int P
		{
			get => (int)GetValue(PProperty);
			set => SetValue(PProperty, value);
		}

		public static DependencyProperty PProperty { get; } = DependencyProperty.Register(
			nameof(P),
			typeof(int),
			typeof(MyButton),
			PropertyMetadata.Create(createDefaultValueCallback: CreateDefaultValue));

		private static object CreateDefaultValue() => _value++;
	}

	[TestMethod]
	public void When_Unsubscribe_From_PropertyChanges()
	{
		var element = new UserControl();
		var changedCount = 0;
		var token = element.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, (_, _) => changedCount++);
		element.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, token);

		element.Visibility = Visibility.Collapsed;

		Assert.AreEqual(0, changedCount);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_CreateDefaultValueCallback()
	{
		var myButton = new MyButton();
		Assert.AreEqual(0, myButton.P);
		Assert.AreEqual(1, myButton.P);
		Assert.AreEqual(2, myButton.P);
		Assert.AreEqual(3, myButton.P);
	}

	private partial class CustomFE : FrameworkElement { }
	private partial class CustomControl : Control { }
	private partial class CustomUserControl : UserControl { }

	private enum TestEnum { A, S, D }

	private partial class EnumDPOwner : FrameworkElement
	{
		public static int ChangeCount;

		public static DependencyProperty EnumValueProperty { get; } = DependencyProperty.Register(
			nameof(EnumValue),
			typeof(TestEnum),
			typeof(EnumDPOwner),
			new PropertyMetadata(TestEnum.A, OnEnumValueChanged));

		public TestEnum EnumValue
		{
			get => (TestEnum)GetValue(EnumValueProperty);
			set => SetValue(EnumValueProperty, value);
		}

		private static void OnEnumValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			ChangeCount++;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/11448")]
	public void When_DP_Set_To_Default_Value_Should_Raise_Changed()
	{
		// Issue #11448: Setting a DP explicitly to its default value should
		// still raise the PropertyChangedCallback. On WinUI it does; on Uno it doesn't.
		EnumDPOwner.ChangeCount = 0;

		var owner = new EnumDPOwner();

		// Set the value to the same as the default (TestEnum.A)
		owner.EnumValue = TestEnum.A;

		// On WinUI, this raises the callback with OldValue=A, NewValue=A
		Assert.AreEqual(1, EnumDPOwner.ChangeCount,
			"Setting a DP explicitly to its default value should raise PropertyChangedCallback (WinUI parity).");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/11448")]
	public void When_DP_Set_To_Different_Value_Should_Raise_Changed()
	{
		// Baseline test: setting a DP to a different value should always raise the callback.
		EnumDPOwner.ChangeCount = 0;

		var owner = new EnumDPOwner();

		owner.EnumValue = TestEnum.S;

		Assert.AreEqual(1, EnumDPOwner.ChangeCount,
			"Setting a DP to a different value should raise PropertyChangedCallback.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_IsTabStop()
	{
		var customControl = new CustomControl();
		Assert.IsTrue(customControl.IsTabStop);

		var userControl = new UserControl();
		Assert.IsFalse(userControl.IsTabStop);

		var customUserControl = new CustomUserControl();
		Assert.IsFalse(customUserControl.IsTabStop);

		Assert.IsFalse((bool)Control.IsTabStopProperty.GetMetadata(typeof(UIElement)).DefaultValue);
		Assert.IsFalse((bool)Control.IsTabStopProperty.GetMetadata(typeof(FrameworkElement)).DefaultValue);
		Assert.IsFalse((bool)Control.IsTabStopProperty.GetMetadata(typeof(CustomFE)).DefaultValue);
		Assert.IsTrue((bool)Control.IsTabStopProperty.GetMetadata(typeof(Control)).DefaultValue);
		Assert.IsTrue((bool)Control.IsTabStopProperty.GetMetadata(typeof(CustomControl)).DefaultValue);
		Assert.IsFalse((bool)Control.IsTabStopProperty.GetMetadata(typeof(UserControl)).DefaultValue);
		Assert.IsFalse((bool)Control.IsTabStopProperty.GetMetadata(typeof(CustomUserControl)).DefaultValue);
	}
}
