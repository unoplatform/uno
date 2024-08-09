using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using FluentAssertions;
using MUXControlsTestApp.Utilities;
using static Private.Infrastructure.TestServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_ItemsPresenter
{
	// Due to physical/logical pixel conversion on Android, measurements aren't exact
	private float Epsilon =>
#if XAMARIN
			0.5f;
#else
		0.0f;
#endif

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Vertical_With_Padding_Only_Panel()
	{
		var grid = (Grid)XamlReader.Load(
		"""
		<Grid x:Name="g" Width="300" Height="300"
			  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
			  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
			  mc:Ignorable="d">
			<Border HorizontalAlignment="Left"
			VerticalAlignment="Top"
			Background="LightBlue"
			BorderBrush="Blue"
			BorderThickness="3">
				<ItemsControl x:Name="ic" Background="Red" Padding="50">
					<ItemsControl.Items>
						<Border Width="40" Height="40" Margin="5" Background="MediumVioletRed" />
						<Border Width="40" Height="40" Margin="5" Background="MediumVioletRed" />
						<Border Width="40" Height="40" Margin="5" Background="MediumVioletRed" />
					</ItemsControl.Items>
					<ItemsControl.Template>
						<ControlTemplate TargetType="ItemsControl">
							<ItemsPresenter Padding="{TemplateBinding Padding}" />
						</ControlTemplate>
					</ItemsControl.Template>
				</ItemsControl>
			</Border>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var ic = (ItemsControl)grid.FindName("ic");
		var ip = grid.FindVisualChildByType<ItemsPresenter>();
		var panel = grid.FindVisualChildByType<StackPanel>();
		var item1 = (FrameworkElement)panel.Children[0];
		var item2 = (FrameworkElement)panel.Children[1];

		// WinUI numbers
		// LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(3, 3, 150, 200), Epsilon);
		// LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 150, 200), Epsilon);
		// ip.DesiredSize.Should().Be(new Size(150, 200), Epsilon);
		// LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(50, 50, 50, 150), Epsilon);
		// LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 50, 50), Epsilon);
		// LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(0, 50, 50, 50), Epsilon);

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(3, 3, 150, 250), Epsilon);
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 150, 250), Epsilon);
		ip.DesiredSize.Should().Be(new Size(150, 250), Epsilon);
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(50, 50, 50, 200), Epsilon);
		LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 50, 50), Epsilon);
		LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(0, 50, 50, 50), Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).Should().BeNull();

		// WinUI numbers
		// ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		// {
		// 	50,
		// 	100,
		// 	150
		// }, Epsilon);
		//
		// ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		// {
		// 	100,
		// 	150,
		// 	200
		// }, Epsilon);
		//
		// ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		// {
		// 	75,
		// 	125,
		// 	175
		// }, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			0,
			50,
			100
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			50,
			100,
			150
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			25,
			75,
			125
		}, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Vertical_With_Padding_Header_Footer()
	{
		var grid = (Grid)XamlReader.Load(
		"""
		<Grid x:Name="g" Width="300" Height="300"
			  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
			  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
			  mc:Ignorable="d">
			<Border BorderBrush="Bisque" BorderThickness="5">
				<ItemsControl x:Name="ic">
					<ItemsControl.Items>
						<Border Width="30" Height="40" Margin="1">
							<Ellipse Fill="Red" />
						</Border>
						<Border Width="30" Height="40" Margin="3">
							<Ellipse Fill="Green" />
						</Border>
					</ItemsControl.Items>
					<ItemsControl.ItemsPanel>
						<ItemsPanelTemplate>
							<StackPanel />
						</ItemsPanelTemplate>
					</ItemsControl.ItemsPanel>
					<ItemsControl.Template>
						<ControlTemplate TargetType="ItemsControl">
							<ItemsPresenter Padding="100">
								<ItemsPresenter.Header>
									<Border Width="30" Height="20">
										<Ellipse Fill="Yellow" />
									</Border>
								</ItemsPresenter.Header>
								<ItemsPresenter.Footer>
									<Border Width="30" Height="20">
										<Ellipse Fill="Pink" />
									</Border>
								</ItemsPresenter.Footer>
							</ItemsPresenter>
						</ControlTemplate>
					</ItemsControl.Template>
				</ItemsControl>
			</Border>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var ic = (ItemsControl)grid.FindName("ic");
		var ip = grid.FindVisualChildByType<ItemsPresenter>();
		var header = (ContentControl)VisualTreeHelper.GetChild(ip, 0);
		var footer = (ContentControl)VisualTreeHelper.GetChild(ip, 2);
		var panel = grid.FindVisualChildByType<StackPanel>();
		var item1 = (FrameworkElement)panel.Children[0];
		var item2 = (FrameworkElement)panel.Children[1];

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(5, 5, 290, 290), Epsilon);
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 290, 290), Epsilon);
		ip.DesiredSize.Should().Be(new Size(236, 288), Epsilon);
		LayoutInformation.GetLayoutSlot(header).Should().Be(new Rect(100, 100, 90, 20), Epsilon);
		LayoutInformation.GetLayoutSlot(footer).Should().Be(new Rect(100, 270, 90, 20), Epsilon);
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(100, 120, 90, 150), Epsilon);
		LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 90, 42), Epsilon);
		LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(0, 42, 90, 46), Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).Should().BeNull();

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			100,
			120,
			162,
			208
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			120,
			162,
			208,
			228
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			110,
			141,
			185,
			218
		}, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Big_Elements_Horizontal_Many_Margins()
	{

		var grid = (Grid)XamlReader.Load(
		"""
		<Grid x:Name="g" Width="600" Height="600"
			  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
			  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
			  mc:Ignorable="d">
			<Border BorderBrush="Bisque" BorderThickness="5">
				<ItemsControl x:Name="ic">
					<ItemsControl.Items>
						<Border Width="400" Height="300" Margin="1">
							<Ellipse Fill="Red" />
						</Border>
						<Border Width="400" Height="300" Margin="3">
							<Ellipse Fill="Green" />
						</Border>
						<Border Width="400" Height="300" VerticalAlignment="Bottom" Margin="9">
							<Ellipse Fill="Blue" />
						</Border>
					</ItemsControl.Items>
					<ItemsControl.ItemsPanel>
						<ItemsPanelTemplate>
							<StackPanel Margin="29" Orientation="Horizontal" />
						</ItemsPanelTemplate>
					</ItemsControl.ItemsPanel>
					<ItemsControl.Template>
						<ControlTemplate TargetType="ItemsControl">
							<ItemsPresenter Margin="47">
								<ItemsPresenter.Header>
									<Border Width="200" Height="200" VerticalAlignment="Bottom" Margin="59">
										<Ellipse Fill="Yellow" />
									</Border>
								</ItemsPresenter.Header>
								<ItemsPresenter.Footer>
									<Border Width="200" Height="200" VerticalAlignment="Top" Margin="39">
										<Ellipse Fill="Pink" />
									</Border>
								</ItemsPresenter.Footer>
							</ItemsPresenter>
						</ControlTemplate>
					</ItemsControl.Template>
				</ItemsControl>
			</Border>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var ic = (ItemsControl)grid.FindName("ic");
		var ip = grid.FindVisualChildByType<ItemsPresenter>();
		var header = (ContentControl)VisualTreeHelper.GetChild(ip, 0);
		var footer = (ContentControl)VisualTreeHelper.GetChild(ip, 2);
		var panel = grid.FindVisualChildByType<StackPanel>();
		var item1 = (FrameworkElement)panel.Children[0];
		var item2 = (FrameworkElement)panel.Children[1];
		var item3 = (FrameworkElement)panel.Children[2];

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(5, 5, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(header).Should().Be(new Rect(0, 0, 318, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(footer).Should().Be(new Rect(814, 0, 278, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(318, 0, 496, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 402, 438), Epsilon);
		LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(402, 0, 406, 438), Epsilon);
		LayoutInformation.GetLayoutSlot(item3).Should().Be(new Rect(808, 0, 418, 438), Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).Should().BeNull();

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			0,
			318,
			749,
			1155,
			814
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			318,
			749,
			1155,
			1573,
			1092
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			159,
			548,
			952,
			1364,
			953
		}, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Big_Elements_Vertical_Many_Margins()
	{
		var grid = (Grid)XamlReader.Load(
		"""
		<Grid x:Name="g" Width="600" Height="600"
			  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
			  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
			  mc:Ignorable="d">
			<Border BorderBrush="Bisque" BorderThickness="5">
				<ItemsControl x:Name="ic">
					<ItemsControl.Items>
						<Border Width="400" Height="300" Margin="1">
							<Ellipse Fill="Red" />
						</Border>
						<Border Width="400" Height="300" Margin="3">
							<Ellipse Fill="Green" />
						</Border>
						<Border Width="400" Height="300" VerticalAlignment="Bottom" Margin="9">
							<Ellipse Fill="Blue" />
						</Border>
					</ItemsControl.Items>
					<ItemsControl.ItemsPanel>
						<ItemsPanelTemplate>
							<StackPanel Margin="29" Orientation="Vertical" />
						</ItemsPanelTemplate>
					</ItemsControl.ItemsPanel>
					<ItemsControl.Template>
						<ControlTemplate TargetType="ItemsControl">
							<ItemsPresenter Margin="47">
								<ItemsPresenter.Header>
									<Border Width="200" Height="200" VerticalAlignment="Bottom" Margin="59">
										<Ellipse Fill="Yellow" />
									</Border>
								</ItemsPresenter.Header>
								<ItemsPresenter.Footer>
									<Border Width="200" Height="200" VerticalAlignment="Top" Margin="39">
										<Ellipse Fill="Pink" />
									</Border>
								</ItemsPresenter.Footer>
							</ItemsPresenter>
						</ControlTemplate>
					</ItemsControl.Template>
				</ItemsControl>
			</Border>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var ic = (ItemsControl)grid.FindName("ic");
		var ip = grid.FindVisualChildByType<ItemsPresenter>();
		var header = (ContentControl)VisualTreeHelper.GetChild(ip, 0);
		var footer = (ContentControl)VisualTreeHelper.GetChild(ip, 2);
		var panel = grid.FindVisualChildByType<StackPanel>();
		var item1 = (FrameworkElement)panel.Children[0];
		var item2 = (FrameworkElement)panel.Children[1];
		var item3 = (FrameworkElement)panel.Children[2];

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(5, 5, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(header).Should().Be(new Rect(0, 0, 496, 318), Epsilon);
		LayoutInformation.GetLayoutSlot(footer).Should().Be(new Rect(0, 814, 496, 278), Epsilon);
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(0, 318, 496, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 438, 302), Epsilon);
		LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(0, 302, 438, 306), Epsilon);
		LayoutInformation.GetLayoutSlot(item3).Should().Be(new Rect(0, 608, 438, 318), Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).Should().BeNull();

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			0,
			318,
			649,
			955,
			814
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			318,
			649,
			955,
			1273,
			1092
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			159,
			498,
			802,
			1114,
			953
		}, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Small_Elements_Horizontal_Many_Margins()
	{
		var grid = (Grid)XamlReader.Load(
		"""
		<Grid x:Name="g" Width="600" Height="600"
		      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		      mc:Ignorable="d">
			<Border BorderBrush="Bisque" BorderThickness="5">
				<ItemsControl x:Name="ic">
					<ItemsControl.Items>
						<Border Width="40" Height="30" Margin="1">
							<Ellipse Fill="Red" />
						</Border>
						<Border Width="40" Height="30" Margin="3">
							<Ellipse Fill="Green" />
						</Border>
						<Border Width="40" Height="30" VerticalAlignment="Bottom" Margin="9">
							<Ellipse Fill="Blue" />
						</Border>
					</ItemsControl.Items>
					<ItemsControl.ItemsPanel>
						<ItemsPanelTemplate>
							<StackPanel Margin="29" Orientation="Horizontal" />
						</ItemsPanelTemplate>
					</ItemsControl.ItemsPanel>
					<ItemsControl.Template>
						<ControlTemplate TargetType="ItemsControl">
							<ItemsPresenter Margin="47">
								<ItemsPresenter.Header>
									<Border Width="20" Height="20" VerticalAlignment="Bottom" Margin="59">
										<Ellipse Fill="Yellow" />
									</Border>
								</ItemsPresenter.Header>
								<ItemsPresenter.Footer>
									<Border Width="20" Height="20" VerticalAlignment="Top" Margin="39">
										<Ellipse Fill="Pink" />
									</Border>
								</ItemsPresenter.Footer>
							</ItemsPresenter>
						</ControlTemplate>
					</ItemsControl.Template>
				</ItemsControl>
			</Border>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var ic = (ItemsControl)grid.FindName("ic");
		var ip = grid.FindVisualChildByType<ItemsPresenter>();
		var header = (ContentControl)VisualTreeHelper.GetChild(ip, 0);
		var footer = (ContentControl)VisualTreeHelper.GetChild(ip, 2);
		var panel = grid.FindVisualChildByType<StackPanel>();
		var item1 = (FrameworkElement)panel.Children[0];
		var item2 = (FrameworkElement)panel.Children[1];
		var item3 = (FrameworkElement)panel.Children[2];

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(5, 5, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(header).Should().Be(new Rect(0, 0, 138, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(footer).Should().Be(new Rect(398, 0, 98, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(138, 0, 260, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 42, 438), Epsilon);
		LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(42, 0, 46, 438), Epsilon);
		LayoutInformation.GetLayoutSlot(item3).Should().Be(new Rect(88, 0, 58, 438), Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).Should().BeNull();

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			0,
			138,
			209,
			255,
			342
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			138,
			209,
			255,
			313,
			440
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			69,
			188,
			232,
			284,
			391
		}, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Small_Elements_Vertical_Many_Margins()
	{
		var grid = (Grid)XamlReader.Load(
		"""
		<Grid x:Name="g" Width="600" Height="600"
			  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
			  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
			  mc:Ignorable="d">
			<Border BorderBrush="Bisque" BorderThickness="5">
				<ItemsControl x:Name="ic">
					<ItemsControl.Items>
						<Border Width="40" Height="30" Margin="1">
							<Ellipse Fill="Red" />
						</Border>
						<Border Width="40" Height="30" Margin="3">
							<Ellipse Fill="Green" />
						</Border>
						<Border Width="40" Height="30" VerticalAlignment="Bottom" Margin="9">
							<Ellipse Fill="Blue" />
						</Border>
					</ItemsControl.Items>
					<ItemsControl.ItemsPanel>
						<ItemsPanelTemplate>
							<StackPanel Margin="29" Orientation="Vertical" />
						</ItemsPanelTemplate>
					</ItemsControl.ItemsPanel>
					<ItemsControl.Template>
						<ControlTemplate TargetType="ItemsControl">
							<ItemsPresenter Margin="47">
								<ItemsPresenter.Header>
									<Border Width="20" Height="20" VerticalAlignment="Bottom" Margin="59">
										<Ellipse Fill="Yellow" />
									</Border>
								</ItemsPresenter.Header>
								<ItemsPresenter.Footer>
									<Border Width="20" Height="20" VerticalAlignment="Top" Margin="39">
										<Ellipse Fill="Pink" />
									</Border>
								</ItemsPresenter.Footer>
							</ItemsPresenter>
						</ControlTemplate>
					</ItemsControl.Template>
				</ItemsControl>
			</Border>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var ic = (ItemsControl)grid.FindName("ic");
		var ip = grid.FindVisualChildByType<ItemsPresenter>();
		var header = (ContentControl)VisualTreeHelper.GetChild(ip, 0);
		var footer = (ContentControl)VisualTreeHelper.GetChild(ip, 2);
		var panel = grid.FindVisualChildByType<StackPanel>();
		var item1 = (FrameworkElement)panel.Children[0];
		var item2 = (FrameworkElement)panel.Children[1];
		var item3 = (FrameworkElement)panel.Children[2];

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(5, 5, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(header).Should().Be(new Rect(0, 0, 496, 138), Epsilon);
		LayoutInformation.GetLayoutSlot(footer).Should().Be(new Rect(0, 398, 496, 98), Epsilon);
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(0, 138, 496, 260), Epsilon);
		LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 438, 32), Epsilon);
		LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(0, 32, 438, 36), Epsilon);
		LayoutInformation.GetLayoutSlot(item3).Should().Be(new Rect(0, 68, 438, 48), Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).Should().BeNull();

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			0,
			138,
			199,
			235,
			312
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			138,
			199,
			235,
			283,
			410
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			69,
			183,
			217,
			259,
			361
		}, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Small_Elements_Horizontal_Many_Margins_No_Footer()
	{
		var grid = (Grid)XamlReader.Load(
		"""
		<Grid x:Name="g" Width="600" Height="600"
			  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
			  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
			  mc:Ignorable="d">
			<Border BorderBrush="Bisque" BorderThickness="5">
				<ItemsControl x:Name="ic">
					<ItemsControl.Items>
						<Border Width="40" Height="30" Margin="1">
							<Ellipse Fill="Red" />
						</Border>
						<Border Width="40" Height="30" Margin="3">
							<Ellipse Fill="Green" />
						</Border>
						<Border Width="40" Height="30" VerticalAlignment="Bottom" Margin="9">
							<Ellipse Fill="Blue" />
						</Border>
					</ItemsControl.Items>
					<ItemsControl.ItemsPanel>
						<ItemsPanelTemplate>
							<StackPanel Margin="29" Orientation="Horizontal" />
						</ItemsPanelTemplate>
					</ItemsControl.ItemsPanel>
					<ItemsControl.Template>
						<ControlTemplate TargetType="ItemsControl">
							<ItemsPresenter Margin="47">
								<ItemsPresenter.Header>
									<Border Width="20" Height="20" VerticalAlignment="Bottom" Margin="59">
										<Ellipse Fill="Yellow" />
									</Border>
								</ItemsPresenter.Header>
							</ItemsPresenter>
						</ControlTemplate>
					</ItemsControl.Template>
				</ItemsControl>
			</Border>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var ic = (ItemsControl)grid.FindName("ic");
		var ip = grid.FindVisualChildByType<ItemsPresenter>();
		var header = (ContentControl)VisualTreeHelper.GetChild(ip, 0);
		var footer = (ContentControl)VisualTreeHelper.GetChild(ip, 2);
		var panel = grid.FindVisualChildByType<StackPanel>();
		var item1 = (FrameworkElement)panel.Children[0];
		var item2 = (FrameworkElement)panel.Children[1];
		var item3 = (FrameworkElement)panel.Children[2];

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(5, 5, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(header).Should().Be(new Rect(0, 0, 138, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(footer).Should().Be(new Rect(496, 0, 0, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(138, 0, 358, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 42, 438), Epsilon);
		LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(42, 0, 46, 438), Epsilon);
		LayoutInformation.GetLayoutSlot(item3).Should().Be(new Rect(88, 0, 58, 438), Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).Should().BeNull();

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			0,
			138,
			209,
			255
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			138,
			209,
			255,
			313
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			69,
			188,
			232,
			284
		}, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Small_Elements_Horizontal_Many_Margins_No_Header()
	{
		var grid = (Grid)XamlReader.Load(
		"""
		<Grid x:Name="g" Width="600" Height="600"
			  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
			  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
			  mc:Ignorable="d">
			<Border BorderBrush="Bisque" BorderThickness="5">
				<ItemsControl x:Name="ic">
					<ItemsControl.Items>
						<Border Width="40" Height="30" Margin="1">
							<Ellipse Fill="Red" />
						</Border>
						<Border Width="40" Height="30" Margin="3">
							<Ellipse Fill="Green" />
						</Border>
						<Border Width="40" Height="30" VerticalAlignment="Bottom" Margin="9">
							<Ellipse Fill="Blue" />
						</Border>
					</ItemsControl.Items>
					<ItemsControl.ItemsPanel>
						<ItemsPanelTemplate>
							<StackPanel Margin="29" Orientation="Horizontal" />
						</ItemsPanelTemplate>
					</ItemsControl.ItemsPanel>
					<ItemsControl.Template>
						<ControlTemplate TargetType="ItemsControl">
							<ItemsPresenter Margin="47">
								<ItemsPresenter.Footer>
									<Border Width="20" Height="20" VerticalAlignment="Top" Margin="39">
										<Ellipse Fill="Pink" />
									</Border>
								</ItemsPresenter.Footer>
							</ItemsPresenter>
						</ControlTemplate>
					</ItemsControl.Template>
				</ItemsControl>
			</Border>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var ic = (ItemsControl)grid.FindName("ic");
		var ip = grid.FindVisualChildByType<ItemsPresenter>();
		var header = (ContentControl)VisualTreeHelper.GetChild(ip, 0);
		var footer = (ContentControl)VisualTreeHelper.GetChild(ip, 2);
		var panel = grid.FindVisualChildByType<StackPanel>();
		var item1 = (FrameworkElement)panel.Children[0];
		var item2 = (FrameworkElement)panel.Children[1];
		var item3 = (FrameworkElement)panel.Children[2];

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(5, 5, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(header).Should().Be(new Rect(0, 0, 0, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(footer).Should().Be(new Rect(398, 0, 98, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(0, 0, 398, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 42, 438), Epsilon);
		LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(42, 0, 46, 438), Epsilon);
		LayoutInformation.GetLayoutSlot(item3).Should().Be(new Rect(88, 0, 58, 438), Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).Should().BeNull();

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			0,
			71,
			117,
			204
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			71,
			117,
			175,
			302
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			50,
			94,
			146,
			253
		}, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Small_Elements_Horizontal_Many_Margins_No_Items()
	{
		var grid = (Grid)XamlReader.Load(
		"""
		<Grid x:Name="g" Width="600" Height="600"
			  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
			  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
			  mc:Ignorable="d">
			<Border BorderBrush="Bisque" BorderThickness="5">
				<ItemsControl x:Name="ic">
					<ItemsControl.ItemsPanel>
						<ItemsPanelTemplate>
							<StackPanel Margin="29" Orientation="Horizontal" />
						</ItemsPanelTemplate>
					</ItemsControl.ItemsPanel>
					<ItemsControl.Template>
						<ControlTemplate TargetType="ItemsControl">
							<ItemsPresenter Margin="47">
								<ItemsPresenter.Header>
									<Border Width="20" Height="20" VerticalAlignment="Bottom" Margin="59">
										<Ellipse Fill="Yellow" />
									</Border>
								</ItemsPresenter.Header>
								<ItemsPresenter.Footer>
									<Border Width="20" Height="20" VerticalAlignment="Top" Margin="39">
										<Ellipse Fill="Pink" />
									</Border>
								</ItemsPresenter.Footer>
							</ItemsPresenter>
						</ControlTemplate>
					</ItemsControl.Template>
				</ItemsControl>
			</Border>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var ic = (ItemsControl)grid.FindName("ic");
		var ip = grid.FindVisualChildByType<ItemsPresenter>();
		var header = (ContentControl)VisualTreeHelper.GetChild(ip, 0);
		var footer = (ContentControl)VisualTreeHelper.GetChild(ip, 2);
		var panel = grid.FindVisualChildByType<StackPanel>();

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(5, 5, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 590, 590), Epsilon);
		LayoutInformation.GetLayoutSlot(header).Should().Be(new Rect(0, 0, 138, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(footer).Should().Be(new Rect(398, 0, 98, 496), Epsilon);
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(138, 0, 260, 496), Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far).Should().BeNull();
		ip.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center).Should().BeNull();

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			0,
			196
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			138,
			294
		}, Epsilon);

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).ToList().Should().BeEquivalentToWithTolerance<float>(new float[]
		{
			69,
			245
		}, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Footer_Binding()
	{
		var ic = (ItemsControl)XamlReader.Load(
			"""
			<ItemsControl
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
				mc:Ignorable="d">
				<ItemsControl.Template>
					<ControlTemplate TargetType="ItemsControl">
						<ItemsPresenter>
							<ItemsPresenter.Footer>
								<TextBlock Text="empty" />
							</ItemsPresenter.Footer>
						</ItemsPresenter>
					</ControlTemplate>
				</ItemsControl.Template>
			</ItemsControl>
			""");

		WindowHelper.WindowContent = ic;
		await WindowHelper.WaitForIdle();

		var tb = ic.FindVisualChildByType<TextBlock>();

		Assert.IsNull(tb.DataContext);

		tb.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyText") });

		ic.DataContext = new MyTextModel("test value");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ic.DataContext, tb.DataContext);
		Assert.AreEqual("test value", tb.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Header_Binding()
	{
		var ic = (ItemsControl)XamlReader.Load(
			"""
			<ItemsControl
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
				mc:Ignorable="d">
				<ItemsControl.Template>
					<ControlTemplate TargetType="ItemsControl">
						<ItemsPresenter>
							<ItemsPresenter.Header>
								<TextBlock Text="empty" />
							</ItemsPresenter.Header>
						</ItemsPresenter>
					</ControlTemplate>
				</ItemsControl.Template>
			</ItemsControl>
			""");

		WindowHelper.WindowContent = ic;
		await WindowHelper.WaitForIdle();

		var tb = ic.FindVisualChildByType<TextBlock>();

		Assert.IsNull(tb.DataContext);

		tb.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyText") });

		ic.DataContext = new MyTextModel("test value");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ic.DataContext, tb.DataContext);
		Assert.AreEqual("test value", tb.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Header_Height_Changed()
	{
		var grid = (Grid)XamlReader.Load(
		"""
		<Grid Height="400"
		      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		      mc:Ignorable="d">
			<ItemsControl>
				<ItemsControl.Template>
					<ControlTemplate TargetType="ItemsControl">
						<ItemsPresenter>
							<ItemsPresenter.Header>
								<TextBlock Text="Before" Height="100" />
							</ItemsPresenter.Header>
						</ItemsPresenter>
					</ControlTemplate>
				</ItemsControl.Template>
			</ItemsControl>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForIdle();

		var tb = grid.FindVisualChildByType<TextBlock>();

		var height = LayoutInformation.GetLayoutSlot(grid.FindVisualChildByType<TextBlock>()).Height;

		grid.FindVisualChildByType<ItemsPresenter>().Header = new TextBlock
		{
			Text = "After",
			Height = 200
		};

		await WindowHelper.WaitForIdle();

		LayoutInformation.GetLayoutSlot(grid.FindVisualChildByType<TextBlock>()).Height.Should().BeApproximately(height + 100, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Footer_Height_Changed()
	{
		var grid = (Grid)XamlReader.Load(
		"""
		<Grid Height="400"
		      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		      mc:Ignorable="d">
			<ItemsControl>
				<ItemsControl.Template>
					<ControlTemplate TargetType="ItemsControl">
						<ItemsPresenter>
							<ItemsPresenter.Footer>
								<TextBlock Text="Before" Height="100" />
							</ItemsPresenter.Footer>
						</ItemsPresenter>
					</ControlTemplate>
				</ItemsControl.Template>
			</ItemsControl>
		</Grid>
		""");

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForIdle();

		var tb = grid.FindVisualChildByType<TextBlock>();

		var top = grid.FindVisualChildByType<TextBlock>().TransformToVisual(grid).TransformPoint(new Point(0, 0)).Y;

		grid.FindVisualChildByType<ItemsPresenter>().Footer = new TextBlock
		{
			Text = "After",
			Height = 200
		};

		await WindowHelper.WaitForIdle();

		grid.FindVisualChildByType<TextBlock>().TransformToVisual(grid).TransformPoint(new Point(0, 0)).Y.Should().BeApproximately(top - 100, Epsilon);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Header_Template()
	{
		var ic = (ItemsControl)XamlReader.Load(
		"""
		<ItemsControl
			xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
			xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
			mc:Ignorable="d">
			<ItemsControl.Template>
				<ControlTemplate TargetType="ItemsControl">
					<ItemsPresenter Header="initial header value">
						<ItemsPresenter.HeaderTemplate>
							<DataTemplate>
								<Border Padding="10">
									<TextBlock Text="{Binding}" />
								</Border>
							</DataTemplate>
						</ItemsPresenter.HeaderTemplate>
					</ItemsPresenter>
				</ControlTemplate>
			</ItemsControl.Template>
		</ItemsControl>
		""");

		WindowHelper.WindowContent = ic;
		await WindowHelper.WaitForIdle();

		var SUT = ic.FindVisualChildByType<ItemsPresenter>();

		Assert.AreEqual(SUT.FindVisualChildByType<TextBlock>().Text, "initial header value");

		SUT.Header = "updated header value";
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(SUT.FindVisualChildByType<TextBlock>().Text, "updated header value");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Footer_Template()
	{
		var ic = (ItemsControl)XamlReader.Load(
		"""
		<ItemsControl
			xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
			xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
			mc:Ignorable="d">
			<ItemsControl.Template>
				<ControlTemplate TargetType="ItemsControl">
					<ItemsPresenter Footer="initial footer value">
						<ItemsPresenter.FooterTemplate>
							<DataTemplate>
								<Border Padding="10">
									<TextBlock Text="{Binding}" />
								</Border>
							</DataTemplate>
						</ItemsPresenter.FooterTemplate>
					</ItemsPresenter>
				</ControlTemplate>
			</ItemsControl.Template>
		</ItemsControl>
		""");

		WindowHelper.WindowContent = ic;
		await WindowHelper.WaitForIdle();

		var SUT = ic.FindVisualChildByType<ItemsPresenter>();

		Assert.AreEqual(SUT.FindVisualChildByType<TextBlock>().Text, "initial footer value");

		SUT.Footer = "updated footer value";
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(SUT.FindVisualChildByType<TextBlock>().Text, "updated footer value");
	}

	public record MyTextModel(string MyText);
}
