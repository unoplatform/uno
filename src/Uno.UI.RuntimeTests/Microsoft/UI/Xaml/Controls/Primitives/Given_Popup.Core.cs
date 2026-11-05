#if HAS_UNO
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives;

/// <summary>
/// Covers what the port of CPopup and CPopupRoot brought: the child sits directly under the popup root.
/// </summary>
[TestClass]
[RunsOnUIThread]
[RequiresFullWindow]
public class Given_Popup_Core
{
	private static Border CreateChild() => new()
	{
		Width = 50,
		Height = 50,
		Background = new SolidColorBrush(Colors.Red),
	};

	private static async Task<Popup> OpenPopup(UIElement child, bool isLightDismiss = false, double x = 0, double y = 0)
	{
		var popup = new Popup
		{
			XamlRoot = WindowHelper.XamlRoot,
			Child = child,
			IsLightDismissEnabled = isLightDismiss,
			HorizontalOffset = x,
			VerticalOffset = y,
		};
		popup.IsOpen = true;
		await WindowHelper.WaitForIdle();
		return popup;
	}

	private static UIElement HitTest(Point point)
		=> VisualTreeHelper.HitTest(point, WindowHelper.XamlRoot.VisualTree.RootElement).element;

	[TestMethod]
	public async Task When_Open_Then_Child_Parent_Is_PopupRoot_And_Closed_Has_None()
	{
		var child = CreateChild();
		var popup = await OpenPopup(child);

		try
		{
			Assert.IsInstanceOfType(VisualTreeHelper.GetParent(child), typeof(PopupRoot));
			Assert.AreEqual(popup, child.Parent);
			Assert.AreEqual(popup, child.GetUIElementAdjustedParentInternal(false));
		}
		finally
		{
			popup.IsOpen = false;
		}

		await WindowHelper.WaitForIdle();

		Assert.IsNull(VisualTreeHelper.GetParent(child));
		Assert.AreEqual(popup, child.Parent);
	}

	[TestMethod]
	public async Task When_Light_Dismiss_Then_Older_Popups_Out_Of_Reach()
	{
		var older = CreateChild();
		var newer = CreateChild();
		var olderPopup = await OpenPopup(older, x: 10, y: 10);
		var newerPopup = await OpenPopup(newer, isLightDismiss: true, x: 200, y: 200);

		try
		{
			Assert.AreEqual(newer, HitTest(new Point(210, 210)));
			Assert.IsInstanceOfType(HitTest(new Point(20, 20)), typeof(PopupRoot));
		}
		finally
		{
			newerPopup.IsOpen = false;
		}

		await WindowHelper.WaitForIdle();

		try
		{
			Assert.AreEqual(older, HitTest(new Point(20, 20)));
			Assert.IsNotInstanceOfType(HitTest(new Point(210, 210)), typeof(PopupRoot));
		}
		finally
		{
			olderPopup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Newer_Popup_Is_Not_Light_Dismiss_Then_It_Stays_Reachable_Above_The_Layer()
	{
		var older = CreateChild();
		var newer = CreateChild();
		var olderPopup = await OpenPopup(older, isLightDismiss: true, x: 10, y: 10);
		var newerPopup = await OpenPopup(newer, x: 200, y: 200);

		try
		{
			Assert.AreEqual(newer, HitTest(new Point(210, 210)));
			Assert.AreEqual(older, HitTest(new Point(20, 20)));
			Assert.IsInstanceOfType(HitTest(new Point(100, 100)), typeof(PopupRoot));
		}
		finally
		{
			newerPopup.IsOpen = false;
			olderPopup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Overlay_Then_Rectangle_Just_Below_Child_And_Not_Hit()
	{
		var other = CreateChild();
		var child = CreateChild();
		var otherPopup = await OpenPopup(other, x: 300, y: 300);
		var popup = new Popup
		{
			XamlRoot = WindowHelper.XamlRoot,
			Child = child,
			LightDismissOverlayMode = LightDismissOverlayMode.On,
		};

		try
		{
			// Only a light-dismiss popup has an overlay.
			popup.IsOpen = true;
			await WindowHelper.WaitForIdle();
			Assert.IsNull(TestServices.Utilities.GetPopupOverlayElement(popup));

			popup.IsLightDismissEnabled = true;
			await WindowHelper.WaitForIdle();

			var overlay = TestServices.Utilities.GetPopupOverlayElement(popup);
			Assert.IsInstanceOfType(overlay, typeof(Rectangle));

			var popupRoot = (PopupRoot)VisualTreeHelper.GetParent(child);
			Assert.AreEqual(popupRoot.Children.IndexOf(child) - 1, popupRoot.Children.IndexOf(overlay));
			Assert.AreEqual(WindowHelper.XamlRoot.Size.Width, overlay.ActualWidth, 1);
			Assert.AreEqual(WindowHelper.XamlRoot.Size.Height, overlay.ActualHeight, 1);

			popup.LightDismissOverlayMode = LightDismissOverlayMode.Off;
			Assert.IsNull(TestServices.Utilities.GetPopupOverlayElement(popup));
			Assert.AreEqual(-1, popupRoot.Children.IndexOf(overlay));
		}
		finally
		{
			popup.IsOpen = false;
			otherPopup.IsOpen = false;
		}
	}

	[TestMethod]
	public void When_LightDismissOverlayMode_Default_Is_Off()
		=> Assert.AreEqual(LightDismissOverlayMode.Off, new Popup().LightDismissOverlayMode);

	[TestMethod]
	public async Task When_Parented_Then_Child_Follows_Ancestor_Transforms_Without_Layout()
	{
		var child = CreateChild();
		var popup = new Popup { Child = child, HorizontalOffset = 10, VerticalOffset = 20 };
		var ancestorTransform = new TranslateTransform { X = 100, Y = 50 };
		var ancestor = new Grid
		{
			Width = 200,
			Height = 200,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			RenderTransform = ancestorTransform,
			Children = { popup },
		};

		WindowHelper.WindowContent = ancestor;
		await WindowHelper.WaitForLoaded(ancestor);

		try
		{
			popup.IsOpen = true;
			await WindowHelper.WaitForIdle();

			var position = child.TransformToVisual(null).TransformPoint(default);
			Assert.AreEqual(110, position.X, 1);
			Assert.AreEqual(70, position.Y, 1);
			Assert.AreEqual(child, HitTest(new Point(115, 75)));

			// No layout pass is involved: the transform chain goes through the popup.
			ancestorTransform.X = 300;

			position = child.TransformToVisual(null).TransformPoint(default);
			Assert.AreEqual(310, position.X, 1);
			Assert.AreEqual(child, HitTest(new Point(315, 75)));

			// A popup's own transform applies to its child.
			popup.RenderTransform = new ScaleTransform { ScaleX = 2, ScaleY = 2 };
			await WindowHelper.WaitForIdle();

			position = child.TransformToVisual(null).TransformPoint(new Point(10, 0));
			Assert.AreEqual(300 + (10 + 10) * 2, position.X, 1);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Unparented_Then_Offset_Is_Window_Relative_And_Popup_Transform_Applies()
	{
		var child = CreateChild();
		var popup = await OpenPopup(child, x: 40, y: 60);

		try
		{
			var position = child.TransformToVisual(null).TransformPoint(default);
			Assert.AreEqual(40, position.X, 1);
			Assert.AreEqual(60, position.Y, 1);

			popup.RenderTransform = new TranslateTransform { X = 5, Y = 5 };
			await WindowHelper.WaitForIdle();

			position = child.TransformToVisual(null).TransformPoint(default);
			Assert.AreEqual(45, position.X, 1);
			Assert.AreEqual(65, position.Y, 1);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Parent_RightToLeft_Then_Offset_Is_Mirrored_And_Is_The_Right_Edge()
	{
		var child = CreateChild();
		var popup = new Popup { Child = child, HorizontalOffset = 30 };
		var parent = new Grid
		{
			Width = 200,
			Height = 200,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			FlowDirection = FlowDirection.RightToLeft,
			Children = { popup },
		};

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		try
		{
			popup.IsOpen = true;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FlowDirection.RightToLeft, ((FrameworkElement)child).FlowDirection);

			// The origin of the child's mirrored space is its right edge.
			var parentRight = parent.TransformToVisual(null).TransformPoint(default).X;
			var position = child.TransformToVisual(null).TransformPoint(default);
			Assert.AreEqual(parentRight - 30, position.X, 1);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Inherited_Properties_Then_Child_Follows_Popup()
	{
		var child = new ContentPresenter();
		var popup = new Popup { Child = child, DataContext = "first", FlowDirection = FlowDirection.RightToLeft, XamlRoot = WindowHelper.XamlRoot };

		// As in WinUI, regardless of the popup being open.
		Assert.AreEqual("first", child.DataContext);
		Assert.AreEqual(FlowDirection.RightToLeft, child.FlowDirection);

		popup.IsOpen = true;
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("first", child.DataContext);

		popup.DataContext = "second";
		Assert.AreEqual("second", child.DataContext);

		popup.IsOpen = false;
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("second", child.DataContext);
		Assert.AreEqual(FlowDirection.RightToLeft, child.FlowDirection);
	}

	[TestMethod]
	public async Task When_Inside_Flyout_Then_Closest_Ancestors_Found()
	{
		var button = new Button { Content = "inner" };
		var target = new Button { Content = "target" };
		var flyout = new Flyout { Content = button };

		WindowHelper.WindowContent = target;
		await WindowHelper.WaitForLoaded(target);

		try
		{
			flyout.ShowAt(target);
			await WindowHelper.WaitForLoaded(button);

			Assert.AreEqual(flyout, Popup.GetClosestFlyoutAncestor(button));
			Assert.IsNotNull(Popup.GetClosestPopupAncestor(button));
			Assert.AreEqual(Popup.GetClosestPopupAncestor(button), PopupRoot.GetOpenPopupForElement(button));
			Assert.IsNull(PopupRoot.GetOpenPopupForElement(target));
		}
		finally
		{
			flyout.Hide();
		}
	}

	[TestMethod]
	public async Task When_GetOpenPopups_Then_Most_Recent_First()
	{
		var first = await OpenPopup(CreateChild());
		var second = await OpenPopup(CreateChild());

		try
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot);
			Assert.AreEqual(second, popups[0]);
			Assert.AreEqual(first, popups[1]);
		}
		finally
		{
			first.IsOpen = false;
			second.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Island_Lost_Focus_Then_Only_Light_Dismiss_Popups_Close()
	{
		var plain = await OpenPopup(CreateChild());
		var lightDismiss = await OpenPopup(CreateChild(), isLightDismiss: true);

		try
		{
			lightDismiss.OnIslandLostFocusImpl();
			plain.OnIslandLostFocusImpl();

			Assert.IsFalse(lightDismiss.IsOpen);
			Assert.IsTrue(plain.IsOpen);
		}
		finally
		{
			lightDismiss.IsOpen = false;
			plain.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Popup_Leaves_Live_Tree_Then_It_Closes()
	{
		var popup = new Popup { Child = CreateChild() };
		var parent = new Grid { Width = 100, Height = 100, Children = { popup } };

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		popup.IsOpen = true;
		await WindowHelper.WaitForIdle();

		parent.Children.Remove(popup);

		Assert.IsFalse(popup.IsOpen);
		Assert.IsFalse(VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Contains(popup));
	}

	[TestMethod]
	public async Task When_Light_Dismiss_Then_PopupRoot_Has_Invokable_Peer()
	{
		var child = CreateChild();
		var popup = await OpenPopup(child, isLightDismiss: true);

		try
		{
			var popupRoot = (PopupRoot)VisualTreeHelper.GetParent(child);
			var peer = new PopupRootAutomationPeer(popupRoot);

			Assert.AreEqual("PopupRoot", peer.GetClassName());
			Assert.AreEqual("Light Dismiss", peer.GetAutomationId());
			Assert.AreEqual(AutomationControlType.Button, peer.GetAutomationControlType());
			Assert.IsFalse(string.IsNullOrEmpty(peer.GetName()));

			var flowsTo = peer.GetFlowsTo();
			Assert.AreEqual(popup.GetOrCreateAutomationPeer(), flowsTo.Single());

			((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();

			Assert.IsFalse(popup.IsOpen);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Content_Dialog_Then_Smoke_Popup_Blocks_Input_Below_Dialog()
	{
		var dialog = new ContentDialog
		{
			XamlRoot = WindowHelper.XamlRoot,
			Content = "text",
			CloseButtonText = "close",
		};

		var below = new Border { Background = new SolidColorBrush(Colors.Green) };
		WindowHelper.WindowContent = below;
		await WindowHelper.WaitForLoaded(below);

		var showTask = dialog.ShowAsync();

		try
		{
			await WindowHelper.WaitFor(() => dialog.IsLoaded);
			await WindowHelper.WaitForIdle();

			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot);
			Assert.AreEqual(2, popups.Count);
			Assert.AreEqual(dialog, popups[0].Child);
			Assert.IsInstanceOfType(popups[1].Child, typeof(Rectangle));

			Assert.AreNotEqual(below, HitTest(new Point(2, 2)));
		}
		finally
		{
			dialog.Hide();
		}

		await WindowHelper.WaitForIdle();
		Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);
		showTask.Cancel();
	}
}
#endif
