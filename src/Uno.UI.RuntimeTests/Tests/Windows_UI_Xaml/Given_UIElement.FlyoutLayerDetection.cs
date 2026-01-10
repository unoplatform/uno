#if HAS_UNO
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public partial class Given_UIElement_FlyoutLayerDetection
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Not_In_Popup_GetRootOfPopupSubTree_Returns_Null()
	{
		var button = new Button { Content = "Test" };
		var border = new Border
		{
			Child = button,
			Width = 200,
			Height = 100
		};

		await UITestHelper.Load(border);

		// Element not in a popup should return null
		Assert.IsNull(button.GetRootOfPopupSubTree(),
			"GetRootOfPopupSubTree should return null for element not in popup");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_In_Popup_GetRootOfPopupSubTree_Returns_Value()
	{
		var popupContent = new Border
		{
			Width = 100,
			Height = 100,
			Child = new Button { Content = "In Popup" }
		};

		var popup = new Popup
		{
			Child = popupContent,
			IsLightDismissEnabled = true
		};

		var hostButton = new Button { Content = "Host" };
		var container = new Grid
		{
			Width = 300,
			Height = 200
		};
		container.Children.Add(hostButton);

		await UITestHelper.Load(container);

		// Open the popup
		popup.IsOpen = true;
		await Task.Delay(100); // Let popup open

		try
		{
			// Element in popup should return non-null
			var popupSubtreeRoot = popupContent.GetRootOfPopupSubTree();
			Assert.IsNotNull(popupSubtreeRoot,
				"GetRootOfPopupSubTree should return non-null for element in popup");
		}
		finally
		{
			popup.IsOpen = false;
		}
	}
}
#endif
