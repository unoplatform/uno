using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.IntegrationTests.dcontrols.stackpanel;

[TestClass]
[RequiresFullWindow]
[RequiresScaling(1.0f)]
public class StackPanelIntegrationTests
{
	private const int s_itemCount = 3;

	[TestMethod]
	public async Task CanStackItemsHorizontally()
	{
		var stackPanel = await PanelsHelper.AddPanelWithContent<StackPanel>(await PanelsHelper.CreateDefaultPanelContent(s_itemCount), Orientation.Horizontal);
		var expectedPositions = new List<Point>();

		for (var i = 0; i < s_itemCount; i++)
		{
			// y-coordinate of each rectangle's origin is 100 because each rectangle is 100 pixels tall, the
			// StackPanel is 300 pixels tall and by default the rectangles are center-aligned vertically
			expectedPositions.Add(new Point(100.0f * i, 100.0f));
		}

		await PanelsHelper.VerifyItemPositions(stackPanel, expectedPositions);
	}

	[TestMethod]
	public async Task CanStackItemsVertically()
	{
		var stackPanel = await PanelsHelper.AddPanelWithContent<StackPanel>(await PanelsHelper.CreateDefaultPanelContent(s_itemCount), Orientation.Vertical);
		var expectedPositions = new List<Point>();

		for (int i = 0; i < s_itemCount; i++)
		{
			// x-coordinate of each rectangle's origin is 100 because each rectangle is 100 pixels wide, the
			// StackPanel is 300 pixels wide and by default the rectangles are center-aligned horizontally
			expectedPositions.Add(new Point(100.0f, 100.0f * i));
		}

		await PanelsHelper.VerifyItemPositions(stackPanel, expectedPositions);
	}

	[TestMethod]
	public async Task CanStackVariableSizedItemsHorizontally()
	{
		List<UIElement> itemsVector = null;

		await RunOnUIThread(() =>
		{
			itemsVector = new List<UIElement>();

			itemsVector.Add(CreateRectangle(100, 50));
			itemsVector.Add(CreateRectangle(300, 200, false));  // collapsed
			itemsVector.Add(CreateRectangle(25, 200));
			itemsVector.Add(CreateRectangle(75, 100));
		});

		var stackPanel = await PanelsHelper.AddPanelWithContent<StackPanel>(itemsVector, Orientation.Horizontal);
		var expectedPositions = new List<Point>();

		// Child rectangles are center-aligned vertically by default
		expectedPositions.Add(new Point(0.0f, 125.0f));
		expectedPositions.Add(new Point(0.0f, 0.0f));
		expectedPositions.Add(new Point(100.0f, 50.0f));
		expectedPositions.Add(new Point(125.0f, 100.0f));

		await PanelsHelper.VerifyItemPositions(stackPanel, expectedPositions);
	}

	[TestMethod]
	public async Task CanStackVariableSizedItemsVertically()
	{
		List<UIElement> itemsVector = null;

		await RunOnUIThread(() =>
		{
			itemsVector = new List<UIElement>();

			itemsVector.Add(CreateRectangle(50, 100));
			itemsVector.Add(CreateRectangle(300, 200, false));  // collapsed
			itemsVector.Add(CreateRectangle(200, 25));
			itemsVector.Add(CreateRectangle(100, 75));
		});

		var stackPanel = await PanelsHelper.AddPanelWithContent<StackPanel>(itemsVector, Orientation.Vertical);
		var expectedPositions = new List<Point>();

		// Child rectangles are center-aligned horizontally by default
		expectedPositions.Add(new Point(125.0f, 0.0f));
		expectedPositions.Add(new Point(0.0f, 0.0f));
		expectedPositions.Add(new Point(50.0f, 100.0f));
		expectedPositions.Add(new Point(100.0f, 125.0f));

		await PanelsHelper.VerifyItemPositions(stackPanel, expectedPositions);
	}

	[TestMethod]
	[RequiresScaling(1.0f)]
	public async Task CanChangeOrientation()
	{
		WindowHelper.SetWindowSizeOverride(new Size(400, 400));

		List<UIElement> itemsVector = null;
		// Build up our vectors of expected rectangle positions
		var expectedVerticalOrientationPositions = new List<Point>();
		{
			// Child rectangles are center-aligned horizontally by default
			expectedVerticalOrientationPositions.Add(new Point(125.0f, 0.0f));
			expectedVerticalOrientationPositions.Add(new Point(50.0f, 100.0f));
			expectedVerticalOrientationPositions.Add(new Point(100.0f, 125.0f));
		}
		var expectedHorizontalOrientationPositions = new List<Point>();
		{
			// Child rectangles are center-aligned vertically by default
			expectedHorizontalOrientationPositions.Add(new Point(0.0f, 100.0f));
			expectedHorizontalOrientationPositions.Add(new Point(50.0f, 137.5f));
			expectedHorizontalOrientationPositions.Add(new Point(250.0f, 112.5f));
		}

		await RunOnUIThread(() =>
		{
			itemsVector = new List<UIElement>();

			itemsVector.Add(CreateRectangle(50, 100));
			itemsVector.Add(CreateRectangle(200, 25));
			itemsVector.Add(CreateRectangle(100, 75));
		});

		// Start with the StackPanel vertically oriented
		var stackPanel = await PanelsHelper.AddPanelWithContent<StackPanel>(itemsVector, Orientation.Vertical);
		await RunOnUIThread(() =>
		{
			stackPanel.UpdateLayout();
		});
		bool layoutUpdated = false;
		await RunOnUIThread(() =>
		{
			stackPanel.LayoutUpdated += (s, e) => layoutUpdated = true;
		});

		await PanelsHelper.VerifyItemPositions(stackPanel, expectedVerticalOrientationPositions);
		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "Vertical");

		LOG_OUTPUT("Changing orientation to horizontal.");

		layoutUpdated = false;
		// Now make the StackPanel horizontally oriented
		await RunOnUIThread(() =>
		{
			stackPanel.Orientation = Orientation.Horizontal;
		});
		await TestServices.WindowHelper.WaitFor(() => layoutUpdated);
		await TestServices.WindowHelper.WaitForIdle();

		await PanelsHelper.VerifyItemPositions(stackPanel, expectedHorizontalOrientationPositions);
		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "Horizontal");

		LOG_OUTPUT("Changing orientation to vertical.");

		layoutUpdated = false;
		// Switch back to vertical orientation
		await RunOnUIThread(() =>
		{
			stackPanel.Orientation = Orientation.Vertical;
		});
		await TestServices.WindowHelper.WaitFor(() => layoutUpdated);
		await TestServices.WindowHelper.WaitForIdle();

		await PanelsHelper.VerifyItemPositions(stackPanel, expectedVerticalOrientationPositions);
		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "Vertical");
	}

	[TestMethod]
	public async Task VerifyDesiredSize_AutoLayout()
	{
		var stackPanel = await VerifyDesiredSize_Setup();
		await PanelsHelper.VerifyPanelDesiredSize(stackPanel, 200.0f, 200.0f);

		LOG_OUTPUT("Changing orientation to horizontal.");

		bool layoutUpdated = false;
		await RunOnUIThread(() =>
		{
			stackPanel.LayoutUpdated += (s, e) => layoutUpdated = true;
		});
		// Now make the StackPanel horizontally oriented
		await RunOnUIThread(() =>
		{
			stackPanel.Orientation = Orientation.Horizontal;
		});
		await TestServices.WindowHelper.WaitFor(() => layoutUpdated);
		await TestServices.WindowHelper.WaitForIdle();
		await PanelsHelper.VerifyPanelDesiredSize(stackPanel, 350.0f, 100.0f);
	}

	[TestMethod]
	public async Task VerifyDesiredSize_MinWidthHeight()
	{
		var stackPanel = await VerifyDesiredSize_Setup();
		await PanelsHelper.VerifyPanelDesiredSize(stackPanel, 200.0f, 200.0f);
		bool layoutUpdated = false;
		await RunOnUIThread(() =>
		{
			stackPanel.LayoutUpdated += (s, e) => layoutUpdated = true;
		});
		// Set MinWidth/MinHeight on StackPanel
		await RunOnUIThread(() =>

		{
			stackPanel.MinWidth = 250;
			stackPanel.MinHeight = 150;
		});
		await TestServices.WindowHelper.WaitFor(() => layoutUpdated);
		await TestServices.WindowHelper.WaitForIdle();
		await PanelsHelper.VerifyPanelDesiredSize(stackPanel, 250.0f, 200.0f);

		LOG_OUTPUT("Changing orientation to horizontal.");

		layoutUpdated = false;
		// Now make the StackPanel horizontally oriented
		await RunOnUIThread(() =>
		{
			stackPanel.Orientation = Orientation.Horizontal;
		});
		await TestServices.WindowHelper.WaitFor(() => layoutUpdated);
		await TestServices.WindowHelper.WaitForIdle();
		await PanelsHelper.VerifyPanelDesiredSize(stackPanel, 350.0f, 150.0f);
	}

	[TestMethod]
	public async Task VerifyDesiredSize_MaxWidthHeight()
	{
		var stackPanel = await VerifyDesiredSize_Setup();
		await PanelsHelper.VerifyPanelDesiredSize(stackPanel, 200.0f, 200.0f);
		bool layoutUpdated = false;
		await RunOnUIThread(() =>
		{
			stackPanel.LayoutUpdated += (s, e) => layoutUpdated = true;
		});
		// Set MinWidth/MinHeight on StackPanel
		await RunOnUIThread(() =>
		{
			stackPanel.MaxWidth = 175;
			stackPanel.MaxHeight = 50;
		});
		await TestServices.WindowHelper.WaitFor(() => layoutUpdated);
		await TestServices.WindowHelper.WaitForIdle();
		await PanelsHelper.VerifyPanelDesiredSize(stackPanel, 175.0f, 50.0f);

		LOG_OUTPUT("Changing orientation to horizontal.");

		layoutUpdated = false;
		// Now make the StackPanel horizontally oriented
		await RunOnUIThread(() =>

		{
			stackPanel.Orientation = Orientation.Horizontal;
		});
		await TestServices.WindowHelper.WaitFor(() => layoutUpdated);
		await TestServices.WindowHelper.WaitForIdle();
		await PanelsHelper.VerifyPanelDesiredSize(stackPanel, 175.0f, 50.0f);
	}

	[TestMethod]
	public async Task ValidateSpacing()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));
		//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

		Grid root = null;
		StackPanel stackPanel = null;

		await RunOnUIThread(() =>
		{
			stackPanel = (StackPanel)XamlReader.Load(
					@"<StackPanel
					    xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			            Background='Yellow'
						Spacing='10'
						HorizontalAlignment='Center'
			            VerticalAlignment='Center'
						Orientation='Vertical'>
					  <Border Opacity='0.5' Height='50' Width='50' Background='Red' />
					  <Border Opacity='0.5' Height='50' Width='50' Background='Blue' />
					  <Border Opacity='0.5' Height='50' Width='50' Background='Green' />
					  <Border Opacity='0.5' Height='50' Width='50' Background='Red' />
					  <Border Opacity='0.5' Height='50' Width='50' Background='Blue' />
					  <Border Opacity='0.5' Height='50' Width='50' Background='Green' />
					</StackPanel>");

			root = new Grid();
			root.Background = new SolidColorBrush(Colors.Purple);
			root.Children.Add(stackPanel);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForLoaded(stackPanel);
		await TestServices.WindowHelper.WaitForIdle();
		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "1");
		bool layoutUpdated = false;
		await RunOnUIThread(() =>
		{
			stackPanel.LayoutUpdated += (s, e) => layoutUpdated = true;
		});
		await RunOnUIThread(() =>

		{
			stackPanel.Spacing = -10;
		});
		await TestServices.WindowHelper.WaitFor(() => layoutUpdated);
		await TestServices.WindowHelper.WaitForIdle();
		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "2");

		layoutUpdated = false;

		await RunOnUIThread(() =>

		{
			stackPanel.Spacing = 10;
			stackPanel.Orientation = Orientation.Horizontal;
		});
		await TestServices.WindowHelper.WaitFor(() => layoutUpdated);
		await TestServices.WindowHelper.WaitForIdle();
		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "3");

		layoutUpdated = false;

		await RunOnUIThread(() =>

		{
			stackPanel.Spacing = -10;
		});
		await TestServices.WindowHelper.WaitFor(() => layoutUpdated);
		await TestServices.WindowHelper.WaitForIdle();
		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "4");
	}

	[TestMethod]
	public async Task VerifyBorderChrome()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(600, 600));
		//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /*resizeWindow*/);
		StackPanel stackPanel = null;
		// Verify that basic layout is performed correctly.
		await RunOnUIThread(() =>

		{
			TestServices.WindowHelper.WindowContent = stackPanel = (StackPanel)XamlReader.Load(
				@"<StackPanel
			    xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
			    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
				VerticalAlignment='Top' HorizontalAlignment='Left'
				Background='Green' BorderThickness='20' BorderBrush='Yellow' CornerRadius='5' Padding='10'>
			    <Rectangle Margin='5' Width='90' Height='40' Fill='Red'/>
			    <Rectangle Margin='5' Width='90' Height='40' Fill='Red'/>
			    <Rectangle Margin='5' Width='90' Height='40' Fill='Red'/>
			    <Rectangle Margin='5' Width='90' Height='40' Fill='Red'/>
			    <Rectangle Margin='5' Width='90' Height='40' Fill='Red'/>
			</StackPanel>");
		});
		await TestServices.WindowHelper.WaitForLoaded(stackPanel);
		await TestServices.WindowHelper.WaitForIdle();

		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
	}

	private async Task<StackPanel> VerifyDesiredSize_Setup()
	{
		List<UIElement> itemsVector = null;

		await RunOnUIThread(() =>

			{
				itemsVector = new List<UIElement>();

				itemsVector.Add(CreateRectangle(50, 100));
				itemsVector.Add(CreateRectangle(300, 200, false));  // collapsed
				itemsVector.Add(CreateRectangle(200, 25));
				itemsVector.Add(CreateRectangle(100, 75));
			});

		var stackPanel = await PanelsHelper.AddPanelWithContent<StackPanel>(itemsVector, Orientation.Vertical);
		await PanelsHelper.VerifyPanelDesiredSize(stackPanel, 300.0f, 300.0f);

		LOG_OUTPUT("Setting StackPanel to var layout.");

		// Set StackPanel to var layout
		await RunOnUIThread(() =>
		{
			stackPanel.ClearValue(StackPanel.WidthProperty);
			stackPanel.ClearValue(StackPanel.HeightProperty);
		});
		await TestServices.WindowHelper.WaitForIdle();

		return stackPanel;
	}

	private Rectangle CreateRectangle(int width, int height, bool visible = true)
	{
		var rectangle = new Rectangle();
		rectangle.Fill = new SolidColorBrush(Colors.Red);
		rectangle.Width = width;
		rectangle.Height = height;

		if (visible)
		{
			rectangle.Visibility = Visibility.Visible;
		}

		else
		{
			rectangle.Visibility = Visibility.Collapsed;
		}

		return rectangle;
	}

	[TestMethod]
	public async Task VerifySnapPoints()
	{
		LOG_OUTPUT("Validating horizontal SnapPoints.");
		await VerifySnapPointsOrientation(Orientation.Horizontal, 50, 25);
		LOG_OUTPUT("Validating vertical SnapPoints.");
		await VerifySnapPointsOrientation(Orientation.Vertical, 50, 25);
	}

	private async Task VerifySnapPointsOrientation(Orientation orientation, int elementWidth, int elementHeight)
	{
		StackPanel stackPanel = null;
		int elementMeasure = orientation == Orientation.Horizontal ? elementWidth : elementHeight;

		await RunOnUIThread(() =>
		{
			stackPanel = new StackPanel();
			stackPanel.AreScrollSnapPointsRegular = true;
			stackPanel.Orientation = orientation;
			TestServices.WindowHelper.WindowContent = stackPanel;
		});

		await TestServices.WindowHelper.WaitForLoaded(stackPanel);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Attach SnapPointsChangedEvent.");

		bool stackPanelHorizontalSnapPointsChangedEvent = false;
		bool stackPanelVerticalSnapPointsChangedEvent = false;
		stackPanel.HorizontalSnapPointsChanged += (s, e) => stackPanelHorizontalSnapPointsChangedEvent = true;
		stackPanel.VerticalSnapPointsChanged += (s, e) => stackPanelVerticalSnapPointsChangedEvent = true;

		await RunOnUIThread(() =>
		{
			stackPanel.Children.Add(CreateRectangle(elementWidth, elementHeight));
			stackPanel.Children.Add(CreateRectangle(elementWidth, elementHeight));
		});

		LOG_OUTPUT("Wait for RegularSnapPointsChangedEvent activation.");

		if (orientation == Orientation.Horizontal)
		{
			await WindowHelper.WaitFor(() => stackPanelHorizontalSnapPointsChangedEvent);
		}
		else
		{
			await WindowHelper.WaitFor(() => stackPanelVerticalSnapPointsChangedEvent);
		}

		await RunOnUIThread(() =>
		{
			float offset = 0;
			var snapRegular = stackPanel.GetRegularSnapPoints(orientation, SnapPointsAlignment.Near, out offset);

			// If regular snap points is enabled, the distance between them should be equal to the width or height depending on the orientation
			VERIFY_ARE_EQUAL(snapRegular, elementMeasure);

			stackPanel.AreScrollSnapPointsRegular = false;
			var snapIrregularNear = stackPanel.GetIrregularSnapPoints(orientation, SnapPointsAlignment.Near);
			var snapIrregularCenter = stackPanel.GetIrregularSnapPoints(orientation, SnapPointsAlignment.Center);
			var snapIrregularFar = stackPanel.GetIrregularSnapPoints(orientation, SnapPointsAlignment.Far);

			VERIFY_ARE_EQUAL(snapIrregularNear.Count, 2);
			VERIFY_ARE_EQUAL(snapIrregularCenter.Count, 2);
			VERIFY_ARE_EQUAL(snapIrregularFar.Count, 2);

			// Verify irregular snap points position for the three variants
			// For SnapPointsAlignment.Near snap points start at the beginning of each element eg: Two elements with width or height 50 will have snap points with value 0 and 50
			// For SnapPointsAlignment.Center snap points start at the center of each element eg: Two elements with width or height 50 will have snap points with value 25 and 75
			// For SnapPointsAlignment.Far snap points start at the end of each element eg: Two elements with width or height 50 will have snap points with value 50 and 100
			for (int i = 0; i < 2; i++)
			{
				VERIFY_ARE_EQUAL(snapIrregularNear[i], elementMeasure * i);
				VERIFY_ARE_EQUAL(snapIrregularCenter[i], elementMeasure * 0.5f + elementMeasure * i);
				VERIFY_ARE_EQUAL(snapIrregularFar[i], elementMeasure * (i + 1));
			}
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Reset SnapPointsChangedEvent.");

		stackPanelHorizontalSnapPointsChangedEvent = false;
		stackPanelVerticalSnapPointsChangedEvent = false;

		await RunOnUIThread(() =>
		{
			stackPanel.Children.Add(CreateRectangle(elementWidth, elementHeight));
		});

		LOG_OUTPUT("Wait for IrregularSnapPointsChangedEvent activation.");

		if (orientation == Orientation.Horizontal)
		{
			await WindowHelper.WaitFor(() => stackPanelHorizontalSnapPointsChangedEvent);
		}
		else
		{
			await WindowHelper.WaitFor(() => stackPanelVerticalSnapPointsChangedEvent);
		}

		await RunOnUIThread(() =>

			{
				var snapIrregularNear = stackPanel.GetIrregularSnapPoints(orientation, SnapPointsAlignment.Near);
				var snapIrregularCenter = stackPanel.GetIrregularSnapPoints(orientation, SnapPointsAlignment.Center);
				var snapIrregularFar = stackPanel.GetIrregularSnapPoints(orientation, SnapPointsAlignment.Far);
				VERIFY_ARE_EQUAL(snapIrregularNear.Count, 3);
				VERIFY_ARE_EQUAL(snapIrregularCenter.Count, 3);
				VERIFY_ARE_EQUAL(snapIrregularFar.Count, 3);

				for (int i = 0; i < 3; i++)
				{
					VERIFY_ARE_EQUAL(snapIrregularNear[i], elementMeasure * i);
					VERIFY_ARE_EQUAL(snapIrregularCenter[i], elementMeasure * 0.5f + elementMeasure * i);
					VERIFY_ARE_EQUAL(snapIrregularFar[i], elementMeasure * (i + 1));
				}
			});
		await TestServices.WindowHelper.WaitForIdle();
	}

	//[TestMethod]
	//public async Task ValidateReorderListViewWithStackPanel()
	//{
	//	// Bug 10619708:Leak: PVLStaggerFunction is leaked, resulting in 75KB???of leaked memory
	//	//TestServices.ErrorHandlingHelper.IgnoreLeaksForTest();

	//	FrameworkElement itemAsFE = null;
	//	ListView listView = null;

	//	await RunOnUIThread(() =>
	//	{
	//		var rootPanel = (Grid)XamlReader.Load(
	//			@"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
	//				    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	//				<ListView x:Name='listView' Width='300' Height='200' CanReorderItems='True' AllowDrop='True'>
	//				    <ListView.ItemsPanel>
	//				        <ItemsPanelTemplate>
	//				            <StackPanel/>
	//				        </ItemsPanelTemplate>
	//				    </ListView.ItemsPanel>
	//				</ListView>
	//			</Grid>");
	//		VERIFY_IS_NOT_NULL(rootPanel);

	//		listView = (ListView)rootPanel.FindName("listView");
	//		VERIFY_IS_NOT_NULL(listView);

	//		var itemsSource = new List<object>();
	//		VERIFY_IS_NOT_NULL(itemsSource);

	//		// populate the items
	//		for (int i = 0; i < 10; ++i)
	//		{
	//			listView.Items.Add(i);
	//		}

	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>
	//	{
	//		listView.SelectedIndex = 0;

	//		// get the first item so we can reorder it
	//		itemAsFE = (FrameworkElement)listView.ContainerFromIndex(0);
	//		VERIFY_IS_NOT_NULL(itemAsFE);
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Drag the first item
	//	TestServices.InputHelper.PressHoldAndPanFromCenter(itemAsFE, 0 /* relX */, 100 /* relY */, 0.1 /* velocityFactor */, 1000 /* holdTime */);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// this applies only to Desktop runs
	//	if (TestServices.Utilities.IsDesktop)
	//	{
	//		// Workaround for TH bug 1491218
	//		TestServices.WindowHelper.RestoreForegroundWindow();
	//	}

	//	await RunOnUIThread(() =>
	//	{
	//		// verify that the SelectedIndex changed to something greater than 0
	//		VERIFY_IS_GREATER_THAN(listView.SelectedIndex, 0);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();
	//}

	[TestMethod]
	public async Task VerifyContentClipping()
	{
		//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree);
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));

		StackPanel stackPanel = null;

		Rectangle rectangle1 = null;
		Rectangle rectangle2 = null;
		Rectangle rectangle3 = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)XamlReader.Load(
				@"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
				    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
				<StackPanel x:Name='stackPanel' Width='100' Height='200' Background='Purple' >
				    <Rectangle x:Name='rectangle1' MinWidth='200' Width='400' Height='12' Fill='Orange' />
				    <Rectangle x:Name='rectangle2' MinWidth='200' Width='400' Height='12' Fill='Orange' />
				    <Rectangle x:Name='rectangle3' MinWidth='200' Width='400' Height='12' Fill='Orange' />
				</StackPanel>
			</Grid>");
			VERIFY_IS_NOT_NULL(rootPanel);

			stackPanel = (StackPanel)rootPanel.FindName("stackPanel");
			VERIFY_IS_NOT_NULL(stackPanel);

			rectangle1 = (Rectangle)rootPanel.FindName("rectangle1");
			VERIFY_IS_NOT_NULL(rectangle1);

			rectangle2 = (Rectangle)rootPanel.FindName("rectangle2");
			VERIFY_IS_NOT_NULL(rectangle2);

			rectangle3 = (Rectangle)(rootPanel.FindName("rectangle3"));
			VERIFY_IS_NOT_NULL(rectangle3);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			stackPanel.Children.RemoveAt(1);
			stackPanel.Children.Insert(0, rectangle2);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.WindowHelper.SynchronouslyTickUIThread(2);
		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
	}

	//[TestMethod]
	//public async Task ValidateBoundingRectangle()
	//{
	//	//if (XamlOneCoreTransforms.IsEnabled())
	//	//{
	//	//	LOG_OUTPUT("This API is not used in OneCoreTransforms mode, skipping test");
	//	//	return;
	//	//}

	//	Windows.Foundation.Size size = new(400, 400);

	//	// We should test this with a scaling factor, but that is currently broken.
	//	// Once that is resolved we should set a 2x scale here to ensure that the UIA bounding rect is getting scaled
	//	// Bug 24280989: [DCPPTest] Xaml tests are failing because Xaml no longer applies the plateau scale
	//	TestServices.WindowHelper.SetWindowSizeOverrideWithScale(size, 1.0f);

	//	StackPanel stackPanel;

	//	await RunOnUIThread(() =>
	//	{
	//		stackPanel = new StackPanel();
	//		stackPanel.HorizontalAlignment = HorizontalAlignment.Left;
	//		stackPanel.VerticalAlignment = VerticalAlignment.Top;
	//		stackPanel.Width = 100;
	//		stackPanel.Height = 100;
	//		stackPanel.Background = new SolidColorBrush(Colors.Purple);

	//		TestServices.WindowHelper.WindowContent = stackPanel;

	//		AutomationProperties.SetName(stackPanel, "stackPanel");
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
	//	{
	//		wrl.ComPtr<IUIAutomationElement> stackPanelElement;
	//		wrl.ComPtr<IUIAutomation> automation;

	//		Automation.AutomationClient.UIAElementInfo uiaInfo;
	//		uiaInfo.m_Name = "stackPanel";

	//		var clientManager = AutomationClient.AutomationClientManager.CreateAutomationClientManagerFromInfo(uiaInfo);

	//		clientManager.GetAutomation(&automation);
	//		clientManager.GetCurrentUIAutomationElement(&stackPanelElement);

	//		VERIFY_IS_NOT_NULL(stackPanelElement);

	//		RECT rect;
	//		VERIFY_SUCCEEDED(stackPanelElement.get_CurrentBoundingRectangle(&rect));

	//		LOG_OUTPUT("get_CurrentBoundingRectangle retrieved RECT of [%d,%d,%d,%d]", rect.left, rect.top, rect.right, rect.bottom);
	//		// The returned rectangle is in screen coordinates which will make the test susceptible to changes in window position/title-bar size.
	//		// Just validate the width/height as that's the main purpose of this test
	//		int width = rect.right - rect.left;
	//		int height = rect.bottom - rect.top;
	//		VERIFY_ARE_EQUAL(width, 100);
	//		VERIFY_ARE_EQUAL(height, 100);
	//	});
	//}

	//[TestMethod]
	//public async Task ValidateBoundingRectangleVisualRelative()
	//{
	//	ValidateBoundingRectangleVisualRelativeInternal(true /*appyScaling*/);
	//}

	//[TestMethod]
	//public async Task ValidateBoundingRectangleVisualRelativeUnscaled()
	//{
	//	ValidateBoundingRectangleVisualRelativeInternal(false /*appyScaling*/);
	//}

	//private void ValidateBoundingRectangleVisualRelativeInternal(bool applyScaling)
	//{
	//	//if (!XamlOneCoreTransforms.IsEnabled())
	//	//{
	//	//	LOG_OUTPUT("Visual Relative mode is only supported in OneCoreTransforms mode, skipping test");
	//	//	return;
	//	//}

	//	ChangeDPI changeDPI(applyScaling? DisplayDPIRange.AboveDefaultFirst : DisplayDPIRange.Default);
	//	TestCleanupWrapper cleanup;

	//	if (applyScaling)
	//	{
	//		Windows.Foundation.Size size = new(400, 400);
	//		TestServices.WindowHelper.SetWindowSizeOverride(size);
	//	}

	//	StackPanel stackPanel;

	//	await RunOnUIThread(() =>
	//	{
	//		stackPanel = new StackPanel();
	//		stackPanel.Width = 100;
	//		stackPanel.Height = 100;
	//		stackPanel.HorizontalAlignment = HorizontalAlignment.Left;
	//		stackPanel.VerticalAlignment = VerticalAlignment.Top;
	//		stackPanel.Background = new SolidColorBrush(Colors.Purple);

	//		TestServices.WindowHelper.WindowContent = stackPanel;

	//		AutomationProperties.SetName(stackPanel, "stackPanel");
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
	//	{
	//		wrl.ComPtr<IUIAutomationElement> stackPanelElement;
	//		wrl.ComPtr<IUIAutomation> automation;

	//		Automation.AutomationClient.UIAElementInfo uiaInfo;
	//		uiaInfo.m_Name = "stackPanel";

	//		var clientManager = AutomationClient.AutomationClientManager.CreateAutomationClientManagerFromInfo(uiaInfo);

	//		clientManager.GetAutomation(&automation);
	//		clientManager.GetCurrentUIAutomationElement(&stackPanelElement);

	//		VERIFY_IS_NOT_NULL(stackPanelElement);

	//		wrl.ComPtr<IUIAutomationElementVisualRelative> stackPanelVisualRelative;
	//		VERIFY_SUCCEEDED(stackPanelElement.As(&stackPanelVisualRelative));

	//		UiaVisualRelativeRectangle visualRelativeRect;
	//		VERIFY_SUCCEEDED(stackPanelVisualRelative.GetVisualRelativeBoundingRectangle(&visualRelativeRect));

	//		LOG_OUTPUT("GetVisualRelativeBoundingRectangle retrieved RECT of [%f,%f,%f,%f]", visualRelativeRect.Rect.left, visualRelativeRect.Rect.top, visualRelativeRect.Rect.width, visualRelativeRect.Rect.height);
	//		VERIFY_ARE_EQUAL(visualRelativeRect.Rect.left, 0);
	//		VERIFY_ARE_EQUAL(visualRelativeRect.Rect.top, 0);
	//		VERIFY_ARE_EQUAL(visualRelativeRect.Rect.width, 100);
	//		VERIFY_ARE_EQUAL(visualRelativeRect.Rect.height, 100);
	//		VERIFY_IS_TRUE(visualRelativeRect.VisualReferenceId.Value! = 0);

	//		UiaVisualRelativePoint centerPoint;
	//		VERIFY_SUCCEEDED(stackPanelVisualRelative.GetVisualRelativeCenterPoint(&centerPoint));
	//		LOG_OUTPUT("GetVisualRelativeCenterPoint retrieved POINT of [%f,%f]", centerPoint.Point.x, centerPoint.Point.y);
	//		VERIFY_ARE_EQUAL(centerPoint.Point.x, 50);
	//		VERIFY_ARE_EQUAL(centerPoint.Point.y, 50);
	//		VERIFY_IS_TRUE(centerPoint.VisualReferenceId.Value! = 0);

	//		UiaVisualRelativePoint clickablePoint;
	//		VERIFY_SUCCEEDED(stackPanelVisualRelative.GetVisualRelativeClickablePoint(&clickablePoint));
	//		LOG_OUTPUT("GetVisualRelativeClickablePoint retrieved POINT of [%f,%f]", clickablePoint.Point.x, clickablePoint.Point.y);
	//		VERIFY_ARE_EQUAL(clickablePoint.Point.x, 50);
	//		VERIFY_ARE_EQUAL(clickablePoint.Point.y, 50);
	//		VERIFY_IS_TRUE(clickablePoint.VisualReferenceId.Value! = 0);
	//	});
}
