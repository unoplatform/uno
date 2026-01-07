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

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContextCanceled_On_Element_Not_In_Popup_Event_Is_Raised()
	{
		var contextCanceledRaised = false;

		var button = new Button { Content = "Test" };
		button.ContextCanceled += (s, e) => contextCanceledRaised = true;

		var border = new Border
		{
			Child = button,
			Width = 200,
			Height = 100
		};

		await UITestHelper.Load(border);

		// Get ContentRoot's ContextMenuProcessor
		var contentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(button);
		Assert.IsNotNull(contentRoot, "ContentRoot should not be null");

		var processor = contentRoot.ContextMenuProcessor;
		Assert.IsNotNull(processor, "ContextMenuProcessor should not be null");

		// Simulate context menu on holding state
		processor.SetIsContextMenuOnHolding(true);

		// Process drag - should raise ContextCanceled since not in popup
		processor.ProcessContextCancelOnDrag(button);

		Assert.IsTrue(contextCanceledRaised, "ContextCanceled event should be raised for element not in popup");
		Assert.IsFalse(processor.IsContextMenuOnHolding, "IsContextMenuOnHolding should be false after drag");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContextCancelOnDrag_Not_In_Holding_State_Nothing_Happens()
	{
		var contextCanceledRaised = false;

		var button = new Button { Content = "Test" };
		button.ContextCanceled += (s, e) => contextCanceledRaised = true;

		var border = new Border
		{
			Child = button,
			Width = 200,
			Height = 100
		};

		await UITestHelper.Load(border);

		// Get ContentRoot's ContextMenuProcessor
		var contentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(button);
		var processor = contentRoot.ContextMenuProcessor;

		// Ensure not in holding state
		processor.SetIsContextMenuOnHolding(false);

		// Process drag - should NOT raise ContextCanceled
		processor.ProcessContextCancelOnDrag(button);

		Assert.IsFalse(contextCanceledRaised,
			"ContextCanceled should not be raised when not in holding state");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContextCanceled_On_Element_In_Popup_Popup_Closes()
	{
		var contextCanceledRaised = false;

		var popupContent = new Border
		{
			Width = 100,
			Height = 100,
		};

		var popupButton = new Button { Content = "In Popup" };
		popupButton.ContextCanceled += (s, e) => contextCanceledRaised = true;
		popupContent.Child = popupButton;

		var popup = new Popup
		{
			Child = popupContent,
			IsLightDismissEnabled = true
		};

		var container = new Grid
		{
			Width = 300,
			Height = 200
		};

		await UITestHelper.Load(container);

		// Open the popup
		popup.IsOpen = true;
		await Task.Delay(100); // Let popup open

		Assert.IsTrue(popup.IsOpen, "Popup should be open");

		try
		{
			// Get ContentRoot's ContextMenuProcessor
			var contentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(popupButton);
			var processor = contentRoot.ContextMenuProcessor;

			// Simulate context menu on holding state
			processor.SetIsContextMenuOnHolding(true);

			// Process drag on element in popup - should close popup instead of raising ContextCanceled
			processor.ProcessContextCancelOnDrag(popupButton);

			await Task.Delay(100); // Let popup close

			// When dragging in popup, popup should close and ContextCanceled should NOT be raised
			Assert.IsFalse(popup.IsOpen, "Popup should be closed after drag in popup");
			Assert.IsFalse(contextCanceledRaised,
				"ContextCanceled should NOT be raised when dragging in popup (popup closes instead)");
			Assert.IsFalse(processor.IsContextMenuOnHolding, "IsContextMenuOnHolding should be false after drag");
		}
		finally
		{
			popup.IsOpen = false;
		}
	}
}
#endif
