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

#if HAS_UNO
	[TestMethod]
	public void When_GetValueUnderPrecedence()
	{
		var grid = new Grid();
		grid.Tag = "LocalValue";

		var (actualValue1, actualPrecedence1) = grid.GetValueUnderPrecedence(FrameworkElement.TagProperty, DependencyPropertyValuePrecedences.Coercion);
		Assert.AreEqual("LocalValue", (string)actualValue1);
		Assert.AreEqual(DependencyPropertyValuePrecedences.Local, actualPrecedence1);

		var (actualValue2, actualPrecedence2) = grid.GetValueUnderPrecedence(FrameworkElement.TagProperty, DependencyPropertyValuePrecedences.Local);
		Assert.IsNull(actualValue2);
		Assert.AreEqual(DependencyPropertyValuePrecedences.DefaultValue, actualPrecedence2);
	}
#endif

	[TestMethod]
	public void When_CreateDefaultValueCallback()
	{
		var myButton = new MyButton();
		Assert.AreEqual(0, myButton.P);
		Assert.AreEqual(1, myButton.P);
		Assert.AreEqual(2, myButton.P);
		Assert.AreEqual(3, myButton.P);
	}
}
