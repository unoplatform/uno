using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ListView_Transitions
{
	private static readonly TimeSpan AnimationsDone = TimeSpan.FromMilliseconds(1300);

	private static DataTemplate Template => (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
		"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Height='40' Text='{Binding}' /></DataTemplate>");

	private static TransitionCollection Transitions(params Transition[] transitions)
	{
		TransitionCollection collection = new();
		collection.AddRange(transitions);
		return collection;
	}

	private static async Task<(ListView List, ObservableCollection<string> Items, ItemsStackPanel Panel)> LoadList(TransitionCollection transitions, int count = 5, bool settle = true)
	{
		ObservableCollection<string> items = new(Enumerable.Range(0, count).Select(i => $"Item {i}"));
		ListView list = new()
		{
			Height = 300,
			Width = 200,
			ItemsSource = items,
			ItemTemplate = Template,
			ItemContainerTransitions = transitions,
		};

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		if (settle)
		{
			await Task.Delay(AnimationsDone);
		}

		return (list, items, (ItemsStackPanel)list.ItemsPanelRoot);
	}

	// Samples what the compositor draws, not a flag: the visual's opacity and transform.
	private static async Task<List<(float Opacity, float OffsetY, float ScaleY)>> Sample(UIElement element, TimeSpan duration)
	{
		List<(float, float, float)> samples = new();
		var stopwatch = Stopwatch.StartNew();
		while (stopwatch.Elapsed < duration)
		{
			var matrix = element.Visual.TransformMatrix;
			samples.Add((element.Visual.Opacity, matrix.M42, matrix.M22));
			await Task.Delay(16);
		}

		return samples;
	}

	private static void AssertAtRest(UIElement element)
	{
		Assert.AreEqual(1f, element.Visual.Opacity, 0.001f, "Opacity should be back to 1");
		var matrix = element.Visual.TransformMatrix;
		Assert.AreEqual(1f, matrix.M22, 0.001f, "Scale should be back to 1");
		Assert.AreEqual(0f, matrix.M42, 0.5f, "Translation should be back to 0");
	}

	[TestMethod]
	public async Task When_First_Load_With_Entrance_Then_Fades_And_Translates()
	{
		var (list, _, _) = await LoadList(Transitions(new EntranceThemeTransition { IsStaggeringEnabled = false }), settle: false);
		var container = (UIElement)list.ContainerFromIndex(0);
		Assert.IsNotNull(container);

		var samples = await Sample(container, AnimationsDone);

		Assert.IsLessThan(0.95f, samples.Min(s => s.Opacity), "Entrance should fade in");
		Assert.IsTrue(samples.Any(s => s.Opacity > 0.05f && s.Opacity < 0.999f), "Opacity should take intermediate values");
		AssertAtRest(container);
	}

	[TestMethod]
	public async Task When_First_Load_With_Staggered_Entrance_Then_Later_Items_Start_Later()
	{
		var (list, _, _) = await LoadList(Transitions(new EntranceThemeTransition { IsStaggeringEnabled = true }), settle: false);

		var first = (UIElement)list.ContainerFromIndex(0);
		var last = (UIElement)list.ContainerFromIndex(4);
		var firstFadesIn = false;
		var lastWasBehind = false;
		var stopwatch = Stopwatch.StartNew();
		while (stopwatch.Elapsed < AnimationsDone)
		{
			firstFadesIn |= first.Visual.Opacity > 0.01f;
			lastWasBehind |= first.Visual.Opacity > last.Visual.Opacity + 0.001f;
			await Task.Delay(16);
		}

		Assert.IsTrue(firstFadesIn);
		Assert.IsTrue(lastWasBehind, "A later item should lag behind the first one");
		AssertAtRest(last);
	}

	[TestMethod]
	public async Task When_Add_Then_Added_Fades_In_And_Neighbour_Moves()
	{
		var (list, items, _) = await LoadList(Transitions(new AddDeleteThemeTransition()));
		var neighbour = (UIElement)list.ContainerFromIndex(2);

		items.Insert(2, "Added");
		await WindowHelper.WaitForIdle();

		var added = (UIElement)list.ContainerFromIndex(2);
		Assert.IsNotNull(added);
		Assert.AreNotSame(neighbour, added);

		var neighbourSamples = Sample(neighbour, AnimationsDone);
		var samples = await Sample(added, AnimationsDone);

		Assert.IsLessThan(0.3f, samples.Min(s => s.Opacity), "The added item should start transparent");
		Assert.IsLessThan(0.99f, samples.Min(s => s.ScaleY), "The added item should scale up");
		AssertAtRest(added);

		Assert.IsTrue((await neighbourSamples).Any(s => Math.Abs(s.OffsetY) > 1f), "The affected neighbour should animate to its new location");
		AssertAtRest(neighbour);
	}

	[TestMethod]
	public async Task When_Remove_Then_Container_Stays_Until_Animation_Ends()
	{
		var (list, items, panel) = await LoadList(Transitions(new AddDeleteThemeTransition()));
		var removed = (UIElement)list.ContainerFromIndex(1);

		items.RemoveAt(1);

		// Out of the collection, but still alive and drawn.
		Assert.IsFalse(panel.Children.Contains(removed));
		Assert.AreSame(panel, VisualTreeHelper.GetParent(removed));
		Assert.AreSame(panel.Visual, removed.Visual.Parent);
		Assert.IsNull(list.ContainerFromItem("Item 1"));

		// The delete animation is 100ms, sampling catches it fading.
		float lowestWhileAttached = 1f;
		var stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < 1000 && ReferenceEquals(removed.Visual.Parent, panel.Visual) && !panel.Children.Contains(removed))
		{
			lowestWhileAttached = Math.Min(lowestWhileAttached, removed.Visual.Opacity);
			await Task.Delay(8);
		}

		await WindowHelper.WaitForIdle();
		Assert.IsLessThan(1f, lowestWhileAttached, "The removed container should fade out while attached");

		// Back in the recycle queue: nothing keeps it realized, and whatever reuses it starts clean.
		Assert.IsNull(list.ContainerFromItem("Item 1"));
		AssertAtRest(removed);
		Assert.AreEqual(4, items.Count);
	}

	[TestMethod]
	public async Task When_Remove_During_Add_Then_Consistent()
	{
		var (list, items, panel) = await LoadList(Transitions(new AddDeleteThemeTransition()));

		items.Insert(1, "Added");
		await WindowHelper.WaitForIdle();
		await Task.Delay(60);
		var added = (UIElement)list.ContainerFromIndex(1);

		items.Remove("Added");
		await WindowHelper.WaitForIdle();
		await Task.Delay(AnimationsDone);

		Assert.AreEqual(5, items.Count);
		for (var i = 0; i < items.Count; i++)
		{
			var container = (UIElement)list.ContainerFromIndex(i);
			Assert.IsNotNull(container);
			AssertAtRest(container);
		}

		AssertAtRest(added);
	}

	[TestMethod]
	public async Task When_Recycling_During_Animation_Then_Container_Comes_Back_Clean()
	{
		var (list, items, panel) = await LoadList(Transitions(new AddDeleteThemeTransition()), count: 200);
		var scrollViewer = FindScrollViewer(list);

		items.Insert(1, "Added");
		await WindowHelper.WaitForIdle();
		var added = (UIElement)list.ContainerFromIndex(1);

		// Scrolled away while it's still fading in.
		scrollViewer.ChangeView(null, 4000, null, disableAnimation: true);
		await WindowHelper.WaitForIdle();
		await Task.Delay(100);
		scrollViewer.ChangeView(null, 0, null, disableAnimation: true);
		await WindowHelper.WaitForIdle();
		await Task.Delay(AnimationsDone);

		foreach (var child in panel.Children)
		{
			AssertAtRest(child);
		}

		AssertAtRest(added);
	}

	[TestMethod]
	public async Task When_Removed_Container_Animating_Then_Not_Reused()
	{
		var (list, items, panel) = await LoadList(Transitions(new AddDeleteThemeTransition()), count: 30);
		var removed = (UIElement)list.ContainerFromIndex(1);

		items.RemoveAt(1);
		items.Insert(1, "Replacement");
		await WindowHelper.WaitForIdle();

		Assert.AreNotSame(removed, list.ContainerFromIndex(1), "A container cannot be reused while its delete animation runs");

		await Task.Delay(AnimationsDone);
		AssertAtRest((UIElement)list.ContainerFromIndex(1));
		AssertAtRest(removed);
	}

	[TestMethod]
	public async Task When_Clear_During_Animations_Then_No_Leftovers()
	{
		var (list, items, panel) = await LoadList(Transitions(new AddDeleteThemeTransition()));
		items.Insert(1, "Added");
		await WindowHelper.WaitForIdle();

		items.Clear();
		await WindowHelper.WaitForIdle();
		await Task.Delay(AnimationsDone);

		Assert.IsNull(list.ContainerFromIndex(0));
		foreach (var child in panel.Children)
		{
			AssertAtRest(child);
		}

		items.Add("Again");
		await WindowHelper.WaitForIdle();
		await Task.Delay(AnimationsDone);
		AssertAtRest((UIElement)list.ContainerFromIndex(0));
	}

	[TestMethod]
	public async Task When_Unloaded_During_Delete_Then_Finishes()
	{
		var (list, items, panel) = await LoadList(Transitions(new AddDeleteThemeTransition()));
		var removed = (UIElement)list.ContainerFromIndex(1);

		items.RemoveAt(1);
		WindowHelper.WindowContent = null;
		await WindowHelper.WaitForIdle();

		Assert.IsNull(removed.Visual.Parent);
		await Task.Delay(300);
		AssertAtRest(removed);
	}

	[TestMethod]
	public async Task When_No_Transitions_Then_No_Delay_And_No_Work()
	{
		var (list, items, panel) = await LoadList(null, settle: false);

		var container = (UIElement)list.ContainerFromIndex(1);
		Assert.IsNull(container.m_pLayoutTransitionStorage, "A list without transitions should not allocate transition state");

		items.RemoveAt(1);

		Assert.IsFalse(ReferenceEquals(panel.Visual, container.Visual.Parent) && !panel.Children.Contains(container), "Removal should be immediate");

		items.Insert(1, "Added");
		await WindowHelper.WaitForIdle();

		var added = (UIElement)list.ContainerFromIndex(1);
		AssertAtRest(added);
		Assert.IsNull(added.m_pLayoutTransitionStorage);
	}

	[TestMethod]
	public async Task When_Empty_Transitions_Then_No_Work()
	{
		var (list, _, _) = await LoadList(new TransitionCollection(), settle: false);

		var container = (UIElement)list.ContainerFromIndex(0);
		Assert.IsNull(container.m_pLayoutTransitionStorage);
	}

	[TestMethod]
	public async Task When_Scrolling_Then_No_Entrance_For_Recycled()
	{
		var (list, _, panel) = await LoadList(
			Transitions(new AddDeleteThemeTransition(), new ContentThemeTransition(), new EntranceThemeTransition { IsStaggeringEnabled = false }),
			count: 200);
		var scrollViewer = FindScrollViewer(list);

		float lowest = 1f;
		for (var offset = 500; offset <= 3000; offset += 500)
		{
			scrollViewer.ChangeView(null, offset, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();

			foreach (var child in panel.Children)
			{
				lowest = Math.Min(lowest, child.Visual.Opacity);
				var matrix = child.Visual.TransformMatrix;
				Assert.AreEqual(0f, matrix.M42, 0.5f, "A realized item shouldn't be animating");
			}
		}

		Assert.AreEqual(1f, lowest, 0.001f, "Scrolling shouldn't play any fade");
	}

	[TestMethod]
	public async Task When_ItemsSource_Replaced_Then_Content_Transition_Plays()
	{
		var (list, _, _) = await LoadList(Transitions(new ContentThemeTransition { VerticalOffset = 40 }));

		list.ItemsSource = new[] { "A", "B", "C" };
		await WindowHelper.WaitForIdle();

		var container = (UIElement)list.ContainerFromIndex(0);
		var samples = await Sample(container, TimeSpan.FromMilliseconds(800));

		Assert.IsTrue(samples.Any(s => s.OffsetY > 0.5f), "Content transition should translate from the offset");
		AssertAtRest(container);
	}

	[TestMethod]
	public async Task When_Reordered_Then_Reorder_Transition_Moves_Affected()
	{
		var (list, items, panel) = await LoadList(Transitions(new ReorderThemeTransition()));
		var affected = (UIElement)list.ContainerFromIndex(1);

		// What ListViewBase does when a reorder completes.
		panel.NotifyOfItemsReordered(1);
		var first = items[0];
		items.RemoveAt(0);
		items.Insert(3, first);
		await WindowHelper.WaitForIdle();

		var samples = await Sample(affected, TimeSpan.FromMilliseconds(600));

		Assert.IsTrue(samples.Any(s => Math.Abs(s.OffsetY) > 1f), "The affected item should animate to its new location");
		AssertAtRest(affected);
	}

	[TestMethod]
	public async Task When_Reposition_Then_Moved_Items_Animate()
	{
		var (list, items, _) = await LoadList(Transitions(new RepositionThemeTransition()));
		var moved = (UIElement)list.ContainerFromIndex(1);

		items.Insert(0, "Added");
		await WindowHelper.WaitForIdle();

		var samples = await Sample(moved, TimeSpan.FromMilliseconds(800));

		Assert.IsTrue(samples.Any(s => Math.Abs(s.OffsetY) > 1f), "A repositioned item should animate");
		AssertAtRest(moved);
	}

	[TestMethod]
	public async Task When_GridView_Add_Then_Animates()
	{
		ObservableCollection<string> items = new(Enumerable.Range(0, 10).Select(i => $"Item {i}"));
		GridView grid = new()
		{
			Height = 300,
			Width = 400,
			ItemsSource = items,
			ItemTemplate = Template,
			ItemContainerTransitions = Transitions(new AddDeleteThemeTransition()),
		};
		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();
		await Task.Delay(AnimationsDone);

		items.Insert(1, "Added");
		await WindowHelper.WaitForIdle();

		var added = (UIElement)grid.ContainerFromIndex(1);
		var samples = await Sample(added, AnimationsDone);

		Assert.IsLessThan(0.3f, samples.Min(s => s.Opacity));
		AssertAtRest(added);
	}

	private static ScrollViewer FindScrollViewer(DependencyObject root)
		=> FindScrollViewerOrNull(root) ?? throw new InvalidOperationException("No ScrollViewer");

	private static ScrollViewer FindScrollViewerOrNull(DependencyObject root)
	{
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if ((child as ScrollViewer ?? FindScrollViewerOrNull(child)) is { } found)
			{
				return found;
			}
		}

		return null;
	}
}
