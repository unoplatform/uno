using System.Collections.Generic;
using System.Threading.Tasks;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Tests.Enterprise;
using static Private.Infrastructure.TestServices;
using System;

namespace Microsoft.UI.Xaml.Tests.Common;

internal static class PanelsHelper
{
	public static async Task<TPanel> AddPanelWithContent<TPanel>(IList<UIElement> contentVector, Orientation orientation)
		where TPanel : Panel, new()
	{
		TPanel panel = null;

		await RunOnUIThread(async () =>
		{
			LOG_OUTPUT("Adding the %s to the visual tree with %d items.", typeof(TPanel).Name, contentVector.Count);
			panel = new TPanel();

			panel.Width = 300;
			panel.Height = 300;

			if (panel is StackPanel sp)
			{
				sp.Orientation = orientation;
			}

			if (panel is VariableSizedWrapGrid vswg)
			{
				vswg.Orientation = orientation;
			}

			foreach (var contentItem in contentVector)
			{
				panel.Children.Add(contentItem);
			}

			TestServices.WindowHelper.WindowContent = panel;
		});

		LOG_OUTPUT("Waiting for the %s to be loaded...", typeof(TPanel).Name);
		await WindowHelper.WaitForLoaded(panel);
		LOG_OUTPUT("%s loaded.", typeof(TPanel).Name);

		await RunOnUIThread(() =>
		{
			panel.UpdateLayout();
		});

		return panel;
	}

	public static async Task<IList<UIElement>> CreateDefaultPanelContent(int numItemsToAdd)
	{
		List<UIElement> itemsVector = null;

		await RunOnUIThread(() =>
		{
			itemsVector = new List<UIElement>();

			for (int i = 0; i < numItemsToAdd; i++)
			{
				var rect = new Rectangle();
				SolidColorBrush redBrush = new SolidColorBrush(Colors.Red);
				SolidColorBrush blueBrush = new SolidColorBrush(Colors.Blue);

				switch (i % 2)
				{
					case 0:
						rect.Fill = redBrush;
						break;
					case 1:
						rect.Fill = blueBrush;
						break;
				}

				rect.Width = 100;
				rect.Height = 100;

				itemsVector.Add(rect);
			}
		});

		return itemsVector.ToArray();
	}

	internal static async Task<TItemsControl> CreateItemsControlWithPanel<TItemsControl, TPanel>(string xamlPanelProperties = "")
		where TItemsControl : ItemsControl, new()
	{
		TItemsControl itemsControl = null;

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Creating a %s with a %s as its panel.", typeof(TItemsControl).Name, typeof(TPanel).Name);

			itemsControl = new TItemsControl();
			itemsControl.Width = 300;
			itemsControl.Height = 300;

			itemsControl.ItemsPanel =
				(ItemsPanelTemplate)XamlReader.Load(
					"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><" + typeof(TPanel).Name + " " + xamlPanelProperties + "/></ItemsPanelTemplate>");

			ApplyContainerStyle(itemsControl);
		});

		return itemsControl;
	}

	internal static async Task<TItemsControl> AddItemsControlWithPanel<TItemsControl, TPanel>(int numItemsToAdd, string xamlPanelProperties = "")
		where TItemsControl : ItemsControl, new()
	{
		TItemsControl itemsControl = await CreateItemsControlWithPanel<TItemsControl, TPanel>(xamlPanelProperties);
		var panelContent = await CreateDefaultPanelContent(numItemsToAdd);
		var loadedRegistration = CreateSafeEventRegistration<ItemsControl, RoutedEventHandler>("Loaded");
		var loadedEvent = new Event();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Adding the %s to the visual tree with %d items.", typeof(TItemsControl).Name, numItemsToAdd);

			itemsControl.ItemsSource = panelContent;

			loadedRegistration.Attach(itemsControl, (sender, e) =>
			{
				loadedEvent.Set();
			});

			TestServices.WindowHelper.WindowContent = itemsControl;
		});

		LOG_OUTPUT("Waiting for %s to be loaded...", typeof(TItemsControl).Name);
		await loadedEvent.WaitForDefault();
		LOG_OUTPUT("%s loaded.", typeof(TItemsControl).Name);

		return itemsControl;
	}

	internal static async Task VerifyItemPositions(Panel panel, IList<Point> expectedPositions)
	{
		await RunOnUIThread(() =>
		{
			VERIFY_IS_LESS_THAN_OR_EQUAL(expectedPositions.Count, panel.Children.Count);

			for (int i = 0; i < expectedPositions.Count; i++)
			{
				var container = panel.Children[i];
				var transform = container.TransformToVisual(panel);
				Point actualPosition = transform.TransformPoint(new Point(0, 0));
				Point expectedPosition = expectedPositions[i];

				VERIFY_ARE_EQUAL(Round(expectedPosition.X), Round(actualPosition.X));
				VERIFY_ARE_EQUAL(Round(expectedPosition.Y), Round(actualPosition.Y));
			}
		});
	}

	internal static async Task VerifyItemPositions(ItemsControl itemsControl, IList<Point> expectedPositions)
	{
		await RunOnUIThread(() =>
		{
			VERIFY_IS_LESS_THAN_OR_EQUAL(expectedPositions.Count, itemsControl.Items.Count);

			for (int i = 0; i < expectedPositions.Count; i++)
			{
				var container = (UIElement)itemsControl.ContainerFromIndex(i);
				var transform = container.TransformToVisual(itemsControl);
				Point actualPosition = transform.TransformPoint(new Point(0, 0));
				Point expectedPosition = expectedPositions[i];

				VERIFY_ARE_EQUAL(Round(expectedPosition.X), Round(actualPosition.X));
				VERIFY_ARE_EQUAL(Round(expectedPosition.Y), Round(actualPosition.Y));
			}
		});
	}

	internal static async Task VerifyPanelDesiredSize(Panel panel, double expectedWidth, double expectedHeight)
	{
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(Round(expectedWidth), Round(panel.DesiredSize.Width));
			VERIFY_ARE_EQUAL(Round(expectedHeight), Round(panel.DesiredSize.Height));
		});
	}

	// Layout rounds midpoints half up (WinUI's XcpRound is floor(x + 0.5)); Math.Round's banker's
	// rounding would expect 112 for a child centred at 112.5 that layout places at 113.
	private static int Round(double value) => (int)Math.Floor(value + 0.5);

	private static void ApplyContainerStyle(ItemsControl itemsControl)
	{
		// There's no container style to apply for ItemsControls, so there's nothing for us to do here.
		if (itemsControl is ListView listView)
		{
			listView.ItemContainerStyle =
				(Style)XamlReader.Load(
					"""
					<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ListViewItem'>
					    <Setter Property='Margin' Value='0'/>
					    <Setter Property='BorderThickness' Value='0'/>
					    <Setter Property='Template'>
					        <Setter.Value>
					            <ControlTemplate TargetType='ListViewItem'>
					                <ListViewItemPresenter
					                    PointerOverBackgroundMargin='0'
					                    ContentMargin='0' />
					            </ControlTemplate>
					        </Setter.Value>
					    </Setter>
					</Style>
					""");
		}
	}
}
