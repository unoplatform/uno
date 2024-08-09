using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_DependencyProperty
{
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
}
