using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_DependencyProperty
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Unsubscribe_From_PropertyChanges()
	{
		var element = new UserControl();
		var changedCount = 0;
		var token = element.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, (_, _) => changedCount++);
		element.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, token);

		element.Visibility = Visibility.Collapsed;

		Assert.AreEqual(0, changedCount);
	}
}
