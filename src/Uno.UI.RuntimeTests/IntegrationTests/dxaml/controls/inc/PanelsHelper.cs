using System.Collections.Generic;
using System.Threading.Tasks;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Tests.Common;

internal static class PanelsHelper
{
	public static async Task<TPanel> AddPanelWithContent<TPanel>(IList<UIElement> contentVector, Orientation orientation)
		where TPanel : Panel, new()
	{
		TPanel panel = null;
		var loadedEvent = false;

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

			panel.Loaded += (s, e) => loadedEvent = true;

			TestServices.WindowHelper.WindowContent = panel;
		});

		LOG_OUTPUT("Waiting for the %s to be loaded...", typeof(TPanel).Name);
		await WindowHelper.WaitFor(() => loadedEvent);
		LOG_OUTPUT("%s loaded.", typeof(TPanel).Name);

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

	//template<class TItemsControl, class TPanel>
	//       static TItemsControl^ CreateItemsControlWithPanel(Platform::String^ xamlPanelProperties = L"")
	//{
	//	TItemsControl ^ itemsControl = nullptr;

	//	await RunOnUIThread(()
	//	{
	//		LOG_OUTPUT("Creating a %s with a %s as its panel.", GetClassName<TItemsControl>().Data(), GetClassName<TPanel>().Data());

	//		itemsControl = ref new TItemsControl();
	//		itemsControl.Width = 300;
	//		itemsControl.Height = 300;

	//		itemsControl.ItemsPanel =
	//			dynamic_cast < ItemsPanelTemplate ^> (XamlReader::Load(
	//				L"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><" + GetClassName<TPanel>() + L" " + xamlPanelProperties + "/></ItemsPanelTemplate>"));

	//		ApplyContainerStyle<TItemsControl>(itemsControl);
	//	});

	//	return itemsControl;
	//}

	//template<class TItemsControl, class TPanel>
	//       static TItemsControl^ AddItemsControlWithPanel(int numItemsToAdd, Platform::String^ xamlPanelProperties = L"")
	//{
	//	TItemsControl ^ itemsControl = CreateItemsControlWithPanel<TItemsControl, TPanel>(xamlPanelProperties);
	//	auto panelContent = CreateDefaultPanelContent(numItemsToAdd);
	//	auto loadedRegistration = CreateSafeEventRegistration(xaml_controls::ItemsControl, Loaded);
	//	auto loadedEvent = std::make_shared<Event>();

	//	RunOnUIThread([&]()

	//		{
	//		LOG_OUTPUT("Adding the %s to the visual tree with %d items.", GetClassName<TItemsControl>().Data(), numItemsToAdd);

	//		itemsControl.ItemsSource = panelContent;

	//		loadedRegistration.Attach(itemsControl, ref new xaml::RoutedEventHandler([loadedEvent](Platform::Object ^ sender, xaml::RoutedEventArgs ^ e)

	//			{
	//			loadedEvent.Set();
	//		}));

	//		TestServices::WindowHelper.WindowContent = itemsControl;
	//	});

	//	LOG_OUTPUT("Waiting for %s to be loaded...", GetClassName<TItemsControl>().Data());
	//	loadedEvent.WaitForDefault();
	//	LOG_OUTPUT("%s loaded.", GetClassName<TItemsControl>().Data());

	//	return itemsControl;
	//}

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

	//internal static void VerifyItemPositions(ItemsControl itemsControl, IList<Point> expectedPositions)
	//{
	//	await RunOnUIThread(() =>
	//	{
	//		VERIFY_IS_LESS_THAN_OR_EQUAL(expectedPositions.Size, itemsControl.Items.Size);

	//		for (unsigned int i = 0; i < expectedPositions.Size; i++)
	//		{
	//			auto container = dynamic_cast < UIElement ^> (itemsControl.ContainerFromIndex(i));
	//			auto transform = container.TransformToVisual(itemsControl);
	//			wf::Point actualPosition = transform.TransformPoint(wf::Point(0, 0));
	//			wf::Point expectedPosition = expectedPositions.GetAt(i);

	//			VERIFY_ARE_EQUAL(Round(expectedPosition.X), Round(actualPosition.X));
	//			VERIFY_ARE_EQUAL(Round(expectedPosition.Y), Round(actualPosition.Y));
	//		}
	//	});
	//}

	internal static async Task VerifyPanelDesiredSize(Panel panel, double expectedWidth, double expectedHeight)
	{
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(Round(expectedWidth), Round(panel.DesiredSize.Width));
			VERIFY_ARE_EQUAL(Round(expectedHeight), Round(panel.DesiredSize.Height));
		});
	}

	private static int Round(double value) => (int)(value + 0.5);

	//template<class TItemsControl>
	//       static void ApplyContainerStyle(TItemsControl^ itemsControl);

	//template<>
	//	static void ApplyContainerStyle<xaml_controls::ItemsControl>(xaml_controls::ItemsControl^ itemsControl)
	//{
	//	// There's no container style to apply for ItemsControls, so there's nothing for us to do here.
	//	// This implementation exists because the method that calls this is called for ItemsControls,
	//	// so we need to provide this implementation.
	//}

	//template<>
	//	static void ApplyContainerStyle<xaml_controls::ListView>(xaml_controls::ListView^ itemsControl)
	//{
	//	itemsControl.ItemContainerStyle =
	//		(Style)XamlReader.Load(
	//			@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ListViewItem'>
	//			    <Setter Property='Margin' Value='0'/>
	//			    <Setter Property='BorderThickness' Value='0'/>
	//			    <Setter Property='Template'>
	//			        <Setter.Value>
	//			            <ControlTemplate TargetType='ListViewItem'>
	//			                <ListViewItemPresenter
	//			                    PointerOverBackgroundMargin='0'
	//			                    ContentMargin='0' />
	//			            </ControlTemplate>
	//			        </Setter.Value>
	//			    </Setter>
	//			</Style>");
	//}
}
