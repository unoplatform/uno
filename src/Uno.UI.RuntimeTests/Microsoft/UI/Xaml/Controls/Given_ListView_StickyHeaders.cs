using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.DevTools.Input;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ListView_StickyHeaders
{
	private const double HeaderHeight = 40;
	private const double ItemHeight = 50;
	private const int ItemsPerGroup = 20;

	private sealed class Group : List<string>
	{
		public string Key { get; init; } = "";
	}

	private static DataTemplate Template(string xaml) => (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
		$"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{xaml}</DataTemplate>");

	private static async Task<(ListView List, ScrollViewer ScrollViewer, Grid Root)> LoadList(string headerBackground = "Red")
	{
		List<Group> groups = new();
		for (var g = 0; g < 5; g++)
		{
			Group group = new() { Key = $"Group {g}" };
			group.AddRange(Enumerable.Range(0, ItemsPerGroup).Select(i => $"G{g}I{i}"));
			groups.Add(group);
		}

		CollectionViewSource source = new() { IsSourceGrouped = true, Source = groups };

		ListView list = new()
		{
			Height = 300,
			Width = 200,
			ItemContainerTransitions = null,
			ItemsSource = source.View,
			ItemTemplate = Template($"<Border Height='{ItemHeight}' Background='Blue' />"),
			ItemContainerStyle = (Style)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ListViewItem'>
					<Setter Property='Padding' Value='0' />
					<Setter Property='HorizontalContentAlignment' Value='Stretch' />
				</Style>
				"""),
		};

		list.GroupStyle.Add(new GroupStyle
		{
			HeaderTemplate = Template($"<Border Height='{HeaderHeight}' HorizontalAlignment='Stretch' Background='{headerBackground}'><TextBlock Text='{{Binding Key}}' /></Border>"),
			HeaderContainerStyle = (Style)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ListViewHeaderItem'>
					<Setter Property='Padding' Value='0' />
					<Setter Property='Margin' Value='0' />
					<Setter Property='MinHeight' Value='0' />
					<Setter Property='HorizontalContentAlignment' Value='Stretch' />
					<Setter Property='Background' Value='Transparent' />
				</Style>
				"""),
		});

		Grid root = new() { Width = 200, Height = 300, Background = new SolidColorBrush(Colors.White) };
		root.Children.Add(list);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		return (list, FindScrollViewer(list), root);
	}

	private static async Task ScrollTo(ScrollViewer scrollViewer, double offset)
	{
		scrollViewer.ChangeView(null, offset, null, disableAnimation: true);
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();
	}

	private static ListViewBaseHeaderItem GetHeader(ListView list, int group)
		=> ((Panel)list.ItemsPanelRoot).Children.OfType<ListViewBaseHeaderItem>().FirstOrDefault(header => header.Content switch
		{
			Group g => g.Key == $"Group {group}",
			ICollectionViewGroup { Group: Group g } => g.Key == $"Group {group}",
			_ => false,
		});

	private static Rect GetBounds(FrameworkElement element, FrameworkElement relativeTo)
		=> element.TransformToVisual(relativeTo).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

	[TestMethod]
	public async Task When_Scrolled_Mid_Group_Then_Header_At_Top()
	{
		var (list, scrollViewer, root) = await LoadList();

		await ScrollTo(scrollViewer, 300);

		var header = GetHeader(list, 0);
		Assert.IsNotNull(header);
		Assert.AreEqual(0, GetBounds(header, root).Y, 1, "The sticky header should sit at the top of the viewport");
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_Scrolled_Mid_Group_Then_Header_Is_Drawn_On_Top()
	{
		var (_, scrollViewer, root) = await LoadList();

		await ScrollTo(scrollViewer, 300);

		var bitmap = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(bitmap, 100, 20, Colors.Red);
		ImageAssert.HasColorAt(bitmap, 100, (float)(HeaderHeight + 15), Colors.Blue);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_Header_Transparent_Then_Items_Do_Not_Show_Through()
	{
		var (list, scrollViewer, root) = await LoadList("Transparent");

		await ScrollTo(scrollViewer, HeaderHeight + ItemHeight * 3 + 30);

		var headerBounds = GetBounds(GetHeader(list, 0), root);
		var items = ((Panel)list.ItemsPanelRoot).Children.OfType<ListViewItem>().Select(item => GetBounds(item, root)).ToArray();
		Assert.IsTrue(
			items.Any(bounds => bounds.Y < headerBounds.Bottom - 2 && bounds.Bottom > headerBounds.Bottom + 2),
			$"An item should straddle the header's bottom edge at {headerBounds.Bottom}");

		var bitmap = await UITestHelper.ScreenShot(root);

		// The item is under the header band, but clipped by it: its top doesn't draw while its bottom does.
		ImageAssert.HasColorAt(bitmap, 150, (float)(headerBounds.Bottom - 15), Colors.White);
		ImageAssert.HasColorAt(bitmap, 150, (float)(headerBounds.Bottom + 1), Colors.Blue);
	}

	[TestMethod]
	public async Task When_Scrolled_Mid_Group_Then_Header_Is_Hit_Test_Target()
	{
		var (list, scrollViewer, root) = await LoadList();

		await ScrollTo(scrollViewer, 300);

		var header = GetHeader(list, 0);
		var selectedBefore = list.SelectedItem;
		var pressed = false;
		header.AddHandler(UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((_, _) => pressed = true), handledEventsToo: true);

		var injector = Windows.UI.Input.Preview.Injection.InputInjector.TryCreate() ?? throw new System.InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		mouse.Press(root.TransformToVisual(null).TransformPoint(new Point(100, 20)));
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(pressed, "The sticky header should take the click, not the item below it");
		Assert.AreSame(selectedBefore, list.SelectedItem, "The item under the header shouldn't be selected");
	}

	[TestMethod]
	public async Task When_Next_Header_Arrives_Then_Header_Is_Pushed_Off()
	{
		var (list, scrollViewer, root) = await LoadList();
		var groupHeight = HeaderHeight + ItemHeight * ItemsPerGroup;

		await ScrollTo(scrollViewer, groupHeight - 150);
		var next = GetHeader(list, 1);
		Assert.IsNotNull(next);
		await ScrollTo(scrollViewer, scrollViewer.VerticalOffset + GetBounds(next, root).Y - 10);

		var headerBounds = GetBounds(GetHeader(list, 0), root);
		var nextBounds = GetBounds(next, root);

		Assert.AreEqual(10, nextBounds.Y, 1, "The next header should be arriving");
		Assert.AreEqual(nextBounds.Y, headerBounds.Bottom, 1, "The sticky header should be pushed by the next one");
		Assert.IsLessThan(0, headerBounds.Y);
	}

	[TestMethod]
	public async Task When_Scrolled_Past_Group_Then_Next_Header_Sticks()
	{
		var (list, scrollViewer, root) = await LoadList();
		var groupHeight = HeaderHeight + ItemHeight * ItemsPerGroup;

		await ScrollTo(scrollViewer, groupHeight + 200);

		Assert.AreEqual(0, GetBounds(GetHeader(list, 1), root).Y, 1);
	}

	[TestMethod]
	public async Task When_Scrolled_Back_Then_Header_Returns_And_Items_Unclipped()
	{
		var (list, scrollViewer, root) = await LoadList();

		await ScrollTo(scrollViewer, 300);
		await ScrollTo(scrollViewer, 0);

		var header = GetHeader(list, 0);
		Assert.AreEqual(0, GetBounds(header, root).Y, 1);

		var panel = (Panel)list.ItemsPanelRoot;
		foreach (var child in panel.Children.OfType<ListViewItem>())
		{
			Assert.IsNull(child.Visual.TransitionClip, "No item should stay clipped once nothing covers it");
		}
	}

	[TestMethod]
	public async Task When_Scrolled_Then_Update_Does_Not_Depend_On_ViewChanging()
	{
		var (list, scrollViewer, root) = await LoadList();
		var header = GetHeader(list, 0);
		foreach (var offset in new[] { 50d, 120d, 240d, 410d })
		{
			await ScrollTo(scrollViewer, offset);
			Assert.AreEqual(0, GetBounds(header, root).Y, 1, $"At offset {offset}");
		}
	}

	private static ScrollViewer FindScrollViewer(DependencyObject root)
		=> TryFind(root) ?? throw new System.InvalidOperationException("No ScrollViewer");

	private static ScrollViewer TryFind(DependencyObject root)
	{
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is ScrollViewer scrollViewer)
			{
				return scrollViewer;
			}

			if (TryFind(child) is { } found)
			{
				return found;
			}
		}

		return null;
	}
}
