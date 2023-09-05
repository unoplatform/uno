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
using Uno.UI.RuntimeTests.Extensions;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_ItemsPresenter
{
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Big_Elements_Horizontal_Many_Margins()
	{
		var grid = (Grid)XamlReader.Load("""
										 <Grid x:Name="g" Width="700" Height="700"
										 	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
										 	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
										 	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
										 	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
										 	mc:Ignorable="d"
										 	>
										 <Border BorderBrush="Bisque" BorderThickness="5">
										      <ItemsControl x:Name="ic">
										          <ItemsControl.Items>
										              <Border Width="400" Height="300" Margin="1">
										                  <Ellipse Fill="Red" />
										              </Border>
										              <Border Width="400" Height="300" Margin="3">
										                  <Ellipse Fill="Green" />
										              </Border>
										              <Border Width="400" Height="300" VerticalAlignment="Bottom"  Margin="9">
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

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(5, 5, 690, 690));
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 690, 690));
		LayoutInformation.GetLayoutSlot(header).Should().Be(new Rect(0, 0, 318, 596));
		LayoutInformation.GetLayoutSlot(footer).Should().Be(new Rect(914, 0, 278, 596));
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(318, 0, 596, 596));
		LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 402, 538));
		LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(402, 0, 406, 538));
		LayoutInformation.GetLayoutSlot(item3).Should().Be(new Rect(808, 0, 418, 538));

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).ToList().Should().BeEquivalentTo(new float[]
		{
			0,
			318,
			749,
			1155,
			914
		});

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).ToList().Should().BeEquivalentTo(new float[]
		{
			318,
			749,
			1155,
			1573,
			1192
		});

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).ToList().Should().BeEquivalentTo(new float[]
		{
			159,
			548,
			952,
			1364,
			1053
		});
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Small_Elements_Horizontal_Many_Margins()
	{
		var grid = (Grid)XamlReader.Load("""
										 <Grid x:Name="g" Width="700" Height="700"
										 	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
										 	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
										 	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
										 	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
										 	mc:Ignorable="d"
										 	>
										 <Border BorderBrush="Bisque" BorderThickness="5">
										      <ItemsControl x:Name="ic">
										          <ItemsControl.Items>
										              <Border Width="40" Height="30" Margin="1">
										                  <Ellipse Fill="Red" />
										              </Border>
										              <Border Width="40" Height="30" Margin="3">
										                  <Ellipse Fill="Green" />
										              </Border>
										              <Border Width="40" Height="30" VerticalAlignment="Bottom"  Margin="9">
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

		LayoutInformation.GetLayoutSlot(ic).Should().Be(new Rect(5, 5, 690, 690));
		LayoutInformation.GetLayoutSlot(ip).Should().Be(new Rect(0, 0, 690, 690));
		LayoutInformation.GetLayoutSlot(header).Should().Be(new Rect(0, 0, 138, 596));
		LayoutInformation.GetLayoutSlot(footer).Should().Be(new Rect(498, 0, 98, 596));
		LayoutInformation.GetLayoutSlot(panel).Should().Be(new Rect(138, 0, 360, 596));
		LayoutInformation.GetLayoutSlot(item1).Should().Be(new Rect(0, 0, 42, 538));
		LayoutInformation.GetLayoutSlot(item2).Should().Be(new Rect(42, 0, 46, 538));
		LayoutInformation.GetLayoutSlot(item3).Should().Be(new Rect(88, 0, 58, 538));

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near).ToList().Should().BeEquivalentTo(new float[]
		{
			0,
			138,
			209,
			255,
			342
		});

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far).ToList().Should().BeEquivalentTo(new float[]
		{
			138,
			209,
			255,
			313,
			440
		});

		ip.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center).ToList().Should().BeEquivalentTo(new float[]
		{
			69,
			188,
			232,
			284,
			391
		});
	}
}
