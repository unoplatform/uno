#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

#if HAS_UNO
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Geometry contract for transformed elements and Skia-WASM semantic nodes. Bounds must compose the
/// complete element-to-root transform chain exactly once and remain synchronized as that chain changes.
/// </summary>
[TestClass]
public class Given_AccessibleGeometry
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Nested_Transforms_Then_TransformToVisual_Composes_Whole_Chain()
	{
		var target = new Border { Width = 40, Height = 20 };
		var inner = new Border
		{
			Margin = new Thickness(7, 11, 0, 0),
			RenderTransform = new TranslateTransform { X = 15, Y = 25 },
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = target
		};
		var outer = new Border
		{
			Margin = new Thickness(3, 5, 0, 0),
			RenderTransform = new TranslateTransform { X = 40, Y = 60 },
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = inner
		};
		var root = new Grid { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { outer } };

		try
		{
			await UITestHelper.Load(root);

			var bounds = target.TransformToVisual(root).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));

			Assert.AreEqual(3 + 40 + 7 + 15, bounds.X, 0.5, "X must compose both margins and both translations exactly once.");
			Assert.AreEqual(5 + 60 + 11 + 25, bounds.Y, 0.5, "Y must compose both margins and both translations exactly once.");
			Assert.AreEqual(40, bounds.Width, 0.5, "Width must be unchanged by translations.");
			Assert.AreEqual(20, bounds.Height, 0.5, "Height must be unchanged by translations.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Ancestor_Scaled_Then_TransformToVisual_Scales_Bounds()
	{
		var target = new Border { Width = 40, Height = 20 };
		var scaled = new Border
		{
			Margin = new Thickness(10, 20, 0, 0),
			RenderTransform = new ScaleTransform { ScaleX = 2, ScaleY = 3 },
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = target
		};
		var root = new Grid { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { scaled } };

		try
		{
			await UITestHelper.Load(root);

			var bounds = target.TransformToVisual(root).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));

			Assert.AreEqual(10, bounds.X, 0.5, "The scale origin is the top-left of the scaled ancestor, which sits at its margin.");
			Assert.AreEqual(20, bounds.Y, 0.5, "The scale origin is the top-left of the scaled ancestor, which sits at its margin.");
			Assert.AreEqual(80, bounds.Width, 0.5, "Width must be scaled by the ancestor ScaleX.");
			Assert.AreEqual(60, bounds.Height, 0.5, "Height must be scaled by the ancestor ScaleY.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow("Scale")]
	[DataRow("Orientation")]
	[DataRow("RotationAngle")]
	[DataRow("RotationAxis")]
	[DataRow("CenterPoint")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Composition_Transform_Changes_Then_Semantic_Rect_Follows(string property)
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();

		var button = CreateNamedButton("Composition Geometry", 120, 40, new Thickness(17, 23, 0, 0));
		var ancestor = new Border
		{
			Width = 260,
			Height = 160,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = button
		};
		var visual = ancestor.Visual;
		if (property == "CenterPoint")
		{
			visual.Scale = new Vector3(2, 3, 1);
		}
		else if (property == "RotationAxis")
		{
			visual.RotationAngle = MathF.PI / 4;
		}

		try
		{
			await UITestHelper.Load(new Grid { Children = { ancestor } });
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the button's semantic node.");
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(SemanticElementExists(ancestor), "The transformed ancestor must be pruned for this scenario.");
			AssertSemanticRectMatchesLayout(button);
			var before = GetLayoutRect(button);
			StartSemanticGeometryCounts(new[] { button });

			void ApplyTransform()
			{
				switch (property)
				{
					case "Scale":
						visual.Scale = new Vector3(2, 3, 1);
						break;
					case "Orientation":
						visual.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 6);
						break;
					case "RotationAngle":
						visual.RotationAngle = MathF.PI / 3;
						break;
					case "RotationAxis":
						visual.RotationAxis = -Vector3.UnitZ;
						break;
					case "CenterPoint":
						visual.CenterPoint = new Vector3(12, 8, 0);
						break;
				}
			}

			ApplyTransform();
			await UITestHelper.WaitForIdle();

			Assert.AreNotEqual(before, GetLayoutRect(button), "The composition property must change the target's geometry.");
			AssertSemanticRectMatchesLayout(button);
			Assert.AreEqual(1, ReadSemanticGeometryCounts().Single(), "A changed transform must refresh the semantic rectangle once.");

			ApplyTransform();
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(1, ReadSemanticGeometryCounts().Single(), "An unchanged transform must not trigger another geometry refresh.");
		}
		finally
		{
			try
			{
				StopSemanticGeometryCounts();
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24280")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_AutoSuggestBox_Or_Ancestor_Transforms_Then_Inner_TextBox_Rect_Follows(bool transformPrunedAncestor, bool rotate)
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();

		var autoSuggestBox = new AutoSuggestBox { Width = 180, Height = 48 };
		var ancestor = new Border
		{
			Margin = new Thickness(60, 70, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = autoSuggestBox
		};

		try
		{
			await UITestHelper.Load(new Grid { Children = { ancestor } });
			var innerTextBox = FindFirstTextBox(autoSuggestBox);
			Assert.IsNotNull(innerTextBox, "The AutoSuggestBox template must contain a TextBox.");
			await UITestHelper.WaitFor(
				() => SemanticElementExists(autoSuggestBox) && SemanticElementExists(innerTextBox),
				timeoutMS: 5000,
				message: "Timed out waiting for both semantic nodes.");
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(SemanticElementExists(ancestor));
			Assert.AreEqual(GetSemanticElementId(autoSuggestBox), GetSemanticParentId(innerTextBox));
			var before = GetRequiredSemanticElementRect(innerTextBox);
			var beforeLayout = GetLayoutRect(innerTextBox);

			FrameworkElement transformed = transformPrunedAncestor ? ancestor : autoSuggestBox;
			transformed.RenderTransformOrigin = new Point(0.5, 0.5);
			transformed.RenderTransform = rotate
				? new RotateTransform { Angle = 30 }
				: new ScaleTransform { ScaleX = 2, ScaleY = 1.5 };
			await UITestHelper.WaitForIdle();

			AssertSemanticRectMatchesLayout(autoSuggestBox);
			AssertSemanticRectChangeMatchesLayout(innerTextBox, before, beforeLayout);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Child_Added_To_Transformed_Semantic_Parent_Then_Rect_Matches_Layout(bool rotate)
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();

		var group = new Grid
		{
			Width = 240,
			Height = 160,
			Margin = new Thickness(60, 70, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			RenderTransformOrigin = new Point(0.5, 0.5),
			RenderTransform = rotate
				? new RotateTransform { Angle = 30 }
				: new ScaleTransform { ScaleX = 2, ScaleY = 1.5 }
		};
		AutomationProperties.SetName(group, "Transformed Semantic Group");
		var button = CreateNamedButton("Transformed Group Child", 90, 36, new Thickness(19, 27, 0, 0));

		try
		{
			await UITestHelper.Load(group);
			await UITestHelper.WaitFor(() => SemanticElementExists(group), timeoutMS: 5000, message: "Timed out waiting for the group's semantic node.");
			await UITestHelper.WaitForIdle();

			group.Children.Add(button);
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the child's semantic node.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual(GetSemanticElementId(group), GetSemanticParentId(button));
			AssertSemanticRectMatchesLayout(group);
			AssertSemanticRectMatchesLayout(button);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ListView_Is_Transformed_Then_Virtualized_Item_Rect_Follows(bool transformBeforeLoad)
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();

		var listView = new ListView
		{
			Width = 240,
			Height = 160,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			ItemsSource = new[] { "First", "Second" }
		};
		var scale = new ScaleTransform { ScaleX = 2, ScaleY = 1.5 };
		if (transformBeforeLoad)
		{
			listView.RenderTransform = scale;
		}

		try
		{
			await UITestHelper.Load(listView);
			await UITestHelper.WaitFor(
				() => listView.ContainerFromIndex(0) is FrameworkElement item && SemanticElementExists(item),
				timeoutMS: 5000,
				message: "Timed out waiting for the realized item's semantic node.");
			await UITestHelper.WaitForIdle();
			var item = (FrameworkElement)listView.ContainerFromIndex(0);

			if (!transformBeforeLoad)
			{
				listView.RenderTransform = scale;
				await UITestHelper.WaitForIdle();
			}

			Assert.AreEqual(GetSemanticElementId(listView), GetSemanticParentId(item));
			AssertSemanticRectMatchesLayout(listView);
			AssertSemanticRectMatchesLayout(item);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Offset_ComboBox_DropDown_Transforms_And_Realizes_Items_Then_Rects_Match_Layout(bool rotate)
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();

		var comboBox = new ComboBox
		{
			Width = 200,
			MaxDropDownHeight = 320,
			Items = { "First", "Second", "Third" },
			SelectedIndex = 0
		};
		AutomationProperties.SetName(comboBox, "Offset Dropdown");
		var ancestor = new Border
		{
			Margin = new Thickness(110, 85, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = comboBox
		};

		try
		{
			await UITestHelper.Load(new Grid { Width = 640, Height = 360, Children = { ancestor } });
			comboBox.Focus(FocusState.Programmatic);
			await UITestHelper.WaitForIdle();
			comboBox.IsDropDownOpen = true;
			await WaitForComboBoxGeometryNodes(comboBox);

			var itemsHost = comboBox.ItemsPanelRoot;
			Assert.IsNotNull(itemsHost);
			var originalBounds = GetLayoutRect(itemsHost);
			Assert.IsTrue(originalBounds.X > 10 && originalBounds.Y > 10, "The dropdown must be offset from the DOM origin.");
			AssertComboBoxDropDownGeometry(comboBox, "initial open");

			var popupRoot = comboBox.GetPopup()?.Child as FrameworkElement;
			Assert.IsNotNull(popupRoot);
			popupRoot.RenderTransformOrigin = new Point(0.25, 0.25);
			popupRoot.RenderTransform = rotate
				? new RotateTransform { Angle = 10 }
				: new ScaleTransform { ScaleX = 1.2, ScaleY = 1.15 };
			await UITestHelper.WaitForIdle();
			Assert.AreNotEqual(originalBounds, GetLayoutRect(itemsHost), "The dropdown ancestor transform must change the items host's bounds.");
			AssertComboBoxDropDownGeometry(comboBox, "transformed dropdown");

			comboBox.Items.Add("Realized While Open");
			await WaitForComboBoxGeometryNodes(comboBox);
			AssertComboBoxDropDownGeometry(comboBox, "new item realized");

			popupRoot.RenderTransform = new TranslateTransform { X = 23, Y = 17 };
			await UITestHelper.WaitForIdle();
			AssertComboBoxDropDownGeometry(comboBox, "moved after realization");

			comboBox.IsDropDownOpen = false;
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(SemanticElementExists(itemsHost), "Closing the dropdown must remove its listbox region.");

			comboBox.IsDropDownOpen = true;
			await WaitForComboBoxGeometryNodes(comboBox);
			AssertComboBoxDropDownGeometry(comboBox, "reopened");
		}
		finally
		{
			comboBox.IsDropDownOpen = false;
			WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Open_ComboBox_Removes_Or_Moves_Explicit_Item_Then_Region_Matches_Items(bool move)
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();

		var first = new ComboBoxItem { Content = "First Explicit" };
		var middle = new ComboBoxItem { Content = "Middle Explicit" };
		var last = new ComboBoxItem { Content = "Last Explicit" };
		var items = new ObservableCollection<ComboBoxItem> { first, middle, last };
		var comboBox = new ComboBox
		{
			Width = 220,
			MaxDropDownHeight = 320,
			Margin = new Thickness(110, 85, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			ItemsSource = items,
			SelectedIndex = 0
		};
		AutomationProperties.SetName(comboBox, "Explicit Dropdown");

		try
		{
			await UITestHelper.Load(new Grid { Width = 640, Height = 360, Children = { comboBox } });
			comboBox.Focus(FocusState.Programmatic);
			await UITestHelper.WaitForIdle();
			comboBox.IsDropDownOpen = true;
			await WaitForComboBoxGeometryNodes(comboBox);
			Assert.AreSame(middle, comboBox.ContainerFromIndex(1));
			Assert.IsTrue(HasRegisteredSemanticElement(middle));
			AssertComboBoxDropDownGeometry(comboBox, "explicit items before mutation");

			if (move)
			{
				items.Move(1, 0);
			}
			else
			{
				items.RemoveAt(1);
			}
			await WaitForComboBoxGeometryNodes(comboBox);

			Assert.AreEqual(move ? 0 : -1, comboBox.IndexFromContainer(middle));
			if (move)
			{
				Assert.AreSame(middle, comboBox.ContainerFromIndex(0));
				Assert.IsTrue(SemanticElementExists(middle), "Moving an explicit container must preserve its option node.");
				Assert.IsTrue(HasRegisteredSemanticElement(middle), "Moving an explicit container must preserve its current region ownership.");
			}
			else
			{
				Assert.AreSame(last, comboBox.ContainerFromIndex(1), "A different explicit container must occupy the removed item's former position.");
				await UITestHelper.WaitFor(
					() => !SemanticElementExists(middle),
					timeoutMS: 5000,
					message: "The removed non-last explicit container left a stale dropdown option.");
				Assert.IsFalse(HasRegisteredSemanticElement(middle), "The removed container must leave the region's membership map.");
			}

			var itemsHost = comboBox.ItemsPanelRoot;
			Assert.IsNotNull(itemsHost);
			Assert.AreEqual(
				items.Count.ToString(CultureInfo.InvariantCulture),
				InvokeBrowserJs($"String(document.getElementById('{GetSemanticElementId(itemsHost)}').querySelectorAll(':scope > [role=\"option\"]').length)"),
				"The listbox must contain exactly the current items, without a stale option.");
			AssertComboBoxDropDownGeometry(comboBox, move ? "explicit container moved" : "explicit container removed");

			var popupRoot = comboBox.GetPopup()?.Child as FrameworkElement;
			Assert.IsNotNull(popupRoot);
			popupRoot.RenderTransform = new ScaleTransform { ScaleX = 1.1, ScaleY = 1.2 };
			await UITestHelper.WaitForIdle();
			AssertComboBoxDropDownGeometry(comboBox, "transformed after explicit mutation");
		}
		finally
		{
			comboBox.IsDropDownOpen = false;
			WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Repeater_Index_Changes_Then_Recycling_Cleans_Current_Ownership()
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();

		var items = new ObservableCollection<string> { "First", "Target", "Last" };
		var repeater = new ItemsRepeater
		{
			ItemsSource = items,
			Layout = new StackLayout(),
			ItemTemplate = new DataTemplate(null, (_, _) => new Button
			{
				Content = "Repeater Item",
				Width = 180,
				Height = 36,
				IsTabStop = false
			})
		};
		AutomationProperties.SetName(repeater, "Index Lifecycle");
		AutomationProperties.SetAccessibilityView(repeater, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Content);
		var root = new Border { Width = 240, Height = 200, Child = repeater };
		FrameworkElement? target = null;
		var preparedAgain = 0;
		(int OldIndex, int NewIndex)? changedIndex = null;
		int? clearingIndex = null;
		void OnPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
		{
			if (ReferenceEquals(args.Element, target))
			{
				preparedAgain++;
			}
		}
		void OnIndexChanged(ItemsRepeater sender, ItemsRepeaterElementIndexChangedEventArgs args)
		{
			if (ReferenceEquals(args.Element, target))
			{
				changedIndex = (args.OldIndex, args.NewIndex);
			}
		}
		void OnClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
		{
			if (ReferenceEquals(args.Element, target))
			{
				clearingIndex = ItemsRepeater.GetVirtualizationInfo(args.Element).Index;
			}
		}

		try
		{
			await UITestHelper.Load(root);
			await WaitForRepeaterGeometryNodes(repeater);
			target = repeater.TryGetElement(1) as FrameworkElement;
			Assert.IsNotNull(target);
			AssertRepeaterRegionMaps(repeater);

			repeater.ElementPrepared += OnPrepared;
			repeater.ElementIndexChanged += OnIndexChanged;
			repeater.ElementClearing += OnClearing;
			items.Insert(0, "Inserted Before");
			await WaitForRepeaterGeometryNodes(repeater);

			Assert.AreSame(target, repeater.TryGetElement(2));
			Assert.AreEqual((1, 2), changedIndex);
			Assert.AreEqual(0, preparedAgain, "Index shifting must not rely on another ElementPrepared callback.");
			var shiftedMaps = ReadRepeaterRegionMaps(repeater);
			var shiftedPosition = GetSemanticAttribute(target, "aria-posinset");
			Console.WriteLine($"Repeater index shift: layout=2, registered={shiftedMaps.ByHandle[target.Visual.Handle]}, preparedAgain={preparedAgain}");

			items.RemoveAt(2);
			await WaitForRepeaterGeometryNodes(repeater);
			Assert.AreEqual(2, clearingIndex, "Recycling must report the index assigned by ElementIndexChanged.");
			Assert.IsFalse(ItemsRepeater.GetVirtualizationInfo(target).IsRealized);
			Assert.AreSame(repeater, VisualTreeHelper.GetParent(target), "The cleared element must remain pooled in the repeater's visual tree.");
			await UITestHelper.WaitFor(
				() => !SemanticElementExists(target),
				timeoutMS: 5000,
				message: "The index-shifted recycled element left a stale semantic node.");
			Assert.IsFalse(HasRegisteredSemanticElement(target), "A pooled element must leave semantic membership.");
			Assert.AreEqual(2, shiftedMaps.ByHandle[target.Visual.Handle]);
			Assert.AreEqual(target.Visual.Handle, shiftedMaps.ByIndex[2]);
			Assert.AreEqual("3", shiftedPosition, "An index change must update the option's one-based position.");
			AssertRepeaterRegionMaps(repeater);

			items.Add("Reused From Pool");
			await WaitForRepeaterGeometryNodes(repeater);
			Assert.AreSame(target, repeater.TryGetElement(3));
			Assert.AreEqual(1, preparedAgain, "Reusing the pooled element must prepare its new ownership once.");
			Assert.AreEqual("4", GetSemanticAttribute(target, "aria-posinset"));
			AssertRepeaterRegionMaps(repeater);

			var region = GetRepeaterSemanticRegion(repeater);
			var unrealize = region.GetType().GetMethod("OnItemUnrealized", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(unrealize);
			unrealize.Invoke(region, new object[] { target.Visual.Handle, 2 });
			await UITestHelper.WaitForIdle();
			Assert.IsTrue(SemanticElementExists(target), "A delayed clearing callback for the old index must not remove the reused element.");
			AssertRepeaterRegionMaps(repeater);
		}
		finally
		{
			repeater.ElementPrepared -= OnPrepared;
			repeater.ElementIndexChanged -= OnIndexChanged;
			repeater.ElementClearing -= OnClearing;
			WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(1, false)]
	[DataRow(100, false)]
	[DataRow(1, true)]
	[DataRow(100, true)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Scroll_Then_Each_Semantic_Descendant_Is_Positioned_Once(int depth, bool horizontal)
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();

		var nodes = new List<Grid>(depth);
		for (var i = 0; i < depth; i++)
		{
			var node = new Grid { Width = 600, Height = 600 };
			AutomationProperties.SetName(node, $"Scroll Geometry {i}");
			if (nodes.Count > 0)
			{
				nodes[^1].Children.Add(node);
			}
			nodes.Add(node);
		}

		var scrollViewer = new ScrollViewer
		{
			Width = 240,
			Height = 120,
			HorizontalScrollMode = horizontal ? ScrollMode.Enabled : ScrollMode.Disabled,
			VerticalScrollMode = horizontal ? ScrollMode.Disabled : ScrollMode.Enabled,
			HorizontalScrollBarVisibility = horizontal ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = horizontal ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Hidden,
			Content = nodes[0]
		};
		var viewChanges = 0;
		void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args) => viewChanges++;

		try
		{
			await UITestHelper.Load(scrollViewer);
			await UITestHelper.WaitFor(
				() => nodes.All(SemanticElementExists),
				timeoutMS: 5000,
				message: "Timed out waiting for the nested semantic nodes.");
			await UITestHelper.WaitForIdle();
			Assert.IsTrue((horizontal ? scrollViewer.ScrollableWidth : scrollViewer.ScrollableHeight) > 30);

			scrollViewer.ViewChanged += OnViewChanged;
			StartSemanticGeometryCounts(nodes);
			Assert.IsTrue(scrollViewer.ChangeView(horizontal ? 30 : null, horizontal ? null : 30, null, disableAnimation: true));
			await UITestHelper.WaitFor(
				() => viewChanges > 0 && Math.Abs((horizontal ? scrollViewer.HorizontalOffset : scrollViewer.VerticalOffset) - 30) < 0.5,
				timeoutMS: 5000,
				message: "The ScrollViewer did not raise its real scroll notification.");
			await UITestHelper.WaitForIdle();

			var counts = ReadSemanticGeometryCounts();
			Console.WriteLine($"Scroll geometry: {depth} nodes, horizontal={horizontal}, {viewChanges} view changes, {counts.Sum()} positioning writes; per-node counts: {string.Join(",", counts)}");
			Assert.AreEqual(1, viewChanges, "This unanimated scroll must exercise one shared scroll traversal.");
			Assert.AreEqual(depth, counts.Length);
			Assert.AreEqual(depth, counts.Sum(), "The shared scroll traversal must position each semantic node exactly once.");
			for (var i = 0; i < nodes.Count; i++)
			{
				Assert.AreEqual(1, counts[i], $"Semantic node {i} was not positioned exactly once.");
				AssertSemanticRectMatchesLayout(nodes[i]);
			}
		}
		finally
		{
			scrollViewer.ViewChanged -= OnViewChanged;
			try
			{
				StopSemanticGeometryCounts();
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Semantic_Subtree_Refreshed_Then_Root_Rect_And_Hidden_State_Are_Preserved()
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();

		var button = CreateNamedButton("Hidden Geometry", 90, 36);
		var collapsedButton = CreateNamedButton("Collapsed Geometry", 90, 36);
		var host = new Grid { Width = 240, Height = 160, Children = { button, collapsedButton } };
		string? rootId = null;

		try
		{
			await UITestHelper.Load(host);
			await UITestHelper.WaitFor(
				() => SemanticElementExists(button) && SemanticElementExists(collapsedButton),
				timeoutMS: 5000,
				message: "Timed out waiting for both semantic nodes.");
			collapsedButton.Visibility = Visibility.Collapsed;
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(SemanticElementExists(collapsedButton), "A collapsed element must leave the semantic tree.");

			button.Visual.IsVisible = false;
			await UITestHelper.WaitForIdle();
			Assert.IsTrue(SemanticElementExists(button), "Composition visibility must hide the existing node without changing XAML ownership.");
			Assert.IsTrue(SemanticElementHasAttribute(button, "hidden"));

			var rootElement = WindowHelper.XamlRoot.VisualTree.RootElement as FrameworkElement;
			Assert.IsNotNull(rootElement);
			rootId = GetSemanticElementId(rootElement);
			Assert.IsTrue(SemanticElementExists(rootElement));

			var instance = GetWebAssemblyAccessibilityInstance();
			var refresh = instance.GetType().GetMethod("UpdateSemanticSubtreeGeometry", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(refresh);

			// Exercise the startup sweep without a size/offset notification repairing the stale root first.
			InvokeBrowserJs($"(function(){{const e = document.getElementById('{rootId}'); e.dataset.geometryTestStyle = e.style.cssText; e.style.left = '53px'; e.style.top = '71px'; e.style.width = '1px'; e.style.height = '1px'; return 'ok';}})()");
			refresh.Invoke(instance, new object[] { rootElement });

			AssertSemanticRectMatchesLayout(rootElement);
			Assert.IsTrue(SemanticElementHasAttribute(button, "hidden"), "A geometry sweep must not reveal a composition-hidden semantic node.");
			Assert.IsFalse(SemanticElementExists(collapsedButton), "A geometry sweep must not recreate a collapsed semantic node.");
		}
		finally
		{
			try
			{
				if (rootId is not null)
				{
					InvokeBrowserJs($"(function(){{const e = document.getElementById('{rootId}'); if (e && e.dataset.geometryTestStyle !== undefined) {{ e.style.cssText = e.dataset.geometryTestStyle; delete e.dataset.geometryTestStyle; }} return 'ok';}})()");
				}
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
#endif
	}

#if HAS_UNO
	private const double Tolerance = 1.5;

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Button_Added_To_Live_Panel_Then_Semantic_Rect_Matches_Layout()
	{
		await EnsureAccessibilityEnabledAsync();

		var host = new Grid { Width = 300, Height = 200, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var button = CreateNamedButton("Geometry Button", 120, 40, new Thickness(37, 23, 0, 0));

		try
		{
			await UITestHelper.Load(host);

			// The parent is live at add time: the node is created immediately and sized by the arrange.
			host.Children.Add(button);
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the button's semantic node.");
			await UITestHelper.WaitForIdle();

			AssertSemanticRectMatchesLayout(button);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Subtree_Built_Detached_Then_Attached_Then_Semantic_Rect_Matches_Layout()
	{
		await EnsureAccessibilityEnabledAsync();

		var button = CreateNamedButton("Detached Then Attached", 90, 36, new Thickness(41, 29, 0, 0));

		// Build the subtree bottom-up while nothing is in the live tree, as generated XAML does.
		var inner = new StackPanel();
		inner.Children.Add(button);
		var outer = new Grid { Width = 300, Height = 200, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		outer.Children.Add(inner);

		try
		{
			await UITestHelper.Load(outer);
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the attached button's semantic node.");
			await UITestHelper.WaitForIdle();

			AssertSemanticRectMatchesLayout(button);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Subtree_Built_Detached_Then_Attached_Then_Node_Is_Under_Semantic_Ancestor()
	{
		await EnsureAccessibilityEnabledAsync();

		var button = CreateNamedButton("Grouped Button");

		// The WASM bridge emits a named, peer-less Panel as a role="group" node (see IsSemanticElement), so
		// the group is the button's nearest semantic ancestor and its node must be the button's DOM parent.
		var group = new StackPanel();
		AutomationProperties.SetName(group, "Detached Group");
		group.Children.Add(button);
		var outer = new Grid();
		outer.Children.Add(group);

		try
		{
			await UITestHelper.Load(outer);
			await UITestHelper.WaitFor(() => SemanticElementExists(button) && SemanticElementExists(group), timeoutMS: 5000, message: "Timed out waiting for the group and button semantic nodes.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual(GetSemanticElementId(group), GetSemanticParentId(button), "A control built inside a detached named group must be parented to that group's semantic node once attached.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Subtree_Built_Detached_Then_Attached_Under_Collapsed_Ancestor_Then_No_Semantic_Node()
	{
		await EnsureAccessibilityEnabledAsync();

		var button = CreateNamedButton("Hidden Ancestor Button");
		var inner = new StackPanel();
		inner.Children.Add(button);
		var hiddenHost = new Border { Visibility = Visibility.Collapsed, Child = inner };

		var sibling = CreateNamedButton("Visible Sibling");

		var panel = new StackPanel();
		panel.Children.Add(hiddenHost);
		panel.Children.Add(sibling);

		try
		{
			await UITestHelper.Load(panel);
			await UITestHelper.WaitFor(() => SemanticElementExists(sibling), timeoutMS: 5000, message: "Timed out waiting for the visible sibling's semantic node.");
			await UITestHelper.WaitForIdle();

			Assert.IsTrue(SemanticElementExists(sibling), "The visible sibling must emit a semantic node.");
			Assert.IsFalse(SemanticElementExists(button), "A control under a Collapsed ancestor must not be exposed: it is never arranged, so its node would be a zero-size phantom at the root.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Child_Added_Under_Live_Collapsed_Ancestor_Then_No_Semantic_Node_Until_Shown()
	{
		await EnsureAccessibilityEnabledAsync();

		var inner = new StackPanel();
		var hiddenHost = new Border { Visibility = Visibility.Collapsed, Child = inner };
		var sibling = CreateNamedButton("Visible Sibling");
		var panel = new StackPanel();
		panel.Children.Add(hiddenHost);
		panel.Children.Add(sibling);

		var button = CreateNamedButton("Late Hidden Ancestor Button", 90, 36);

		try
		{
			await UITestHelper.Load(panel);
			await UITestHelper.WaitFor(() => SemanticElementExists(sibling), timeoutMS: 5000, message: "Timed out waiting for the visible sibling's semantic node.");

			// The parent is live but sits under a Collapsed ancestor: nothing may be emitted yet.
			inner.Children.Add(button);
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(SemanticElementExists(button), "A control added under a live Collapsed ancestor must not be exposed while the ancestor is hidden.");

			hiddenHost.Visibility = Visibility.Visible;
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic node once the ancestor was shown.");
			await UITestHelper.WaitForIdle();

			AssertSemanticRectMatchesLayout(button);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24280")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Pruned_Ancestor_Moves_Then_Semantic_Rect_Follows()
	{
		var button = new Button { Content = "Target" };
		var pruned = new Border { Child = button, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var root = new Grid { Children = { pruned } };

		try
		{
			await UITestHelper.Load(root);

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic element to be created.");
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(SemanticElementExists(pruned), "The Border is expected to be pruned from the accessibility tree for this scenario to be meaningful.");

			var before = GetRequiredSemanticElementRect(button);

			pruned.Margin = new Thickness(0, 120, 0, 0);
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitFor(() => Math.Abs(GetRequiredSemanticElementRect(button).Y - before.Y) > 1, timeoutMS: 5000, message: "The semantic rectangle never followed the pruned ancestor.");

			var after = GetRequiredSemanticElementRect(button);

			Assert.AreEqual(120, after.Y - before.Y, 2, "The semantic rectangle must follow the offset of a pruned ancestor.");
			Assert.AreEqual(0, after.X - before.X, 2, "A vertical move must not shift the semantic rectangle horizontally.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24280")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Nested_Ancestors_Are_Transformed_Then_Semantic_Rect_Follows()
	{
		var button = new Button { Content = "Target" };
		var inner = new Border { Child = button, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var outer = new Border { Child = inner, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var root = new Grid { Children = { outer } };

		try
		{
			await UITestHelper.Load(root);

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic element to be created.");
			await UITestHelper.WaitForIdle();

			var before = GetRequiredSemanticElementRect(button);

			outer.RenderTransform = new TranslateTransform { X = 40, Y = 60 };
			inner.RenderTransform = new TranslateTransform { X = 15, Y = 25 };
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitFor(() => Math.Abs(GetRequiredSemanticElementRect(button).Y - before.Y) > 1, timeoutMS: 5000, message: "The semantic rectangle never followed the ancestor render transforms.");

			var after = GetRequiredSemanticElementRect(button);

			Assert.AreEqual(55, after.X - before.X, 2, "Both ancestor translations must be composed on X.");
			Assert.AreEqual(85, after.Y - before.Y, 2, "Both ancestor translations must be composed on Y.");
			Assert.AreEqual(before.Width, after.Width, 2, "A translation must not resize the semantic rectangle.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Ancestor_Is_Scaled_Then_Semantic_Rect_Is_Scaled()
	{
		var button = new Button { Content = "Target" };
		var scaled = new Border { Child = button, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var root = new Grid { Children = { scaled } };

		try
		{
			await UITestHelper.Load(root);

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic element to be created.");
			await UITestHelper.WaitForIdle();

			var before = GetRequiredSemanticElementRect(button);
			Assert.IsTrue(before.Width > 0, "The semantic rectangle must have a non-zero width before scaling.");

			scaled.RenderTransform = new ScaleTransform { ScaleX = 2, ScaleY = 2 };
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitFor(() => GetRequiredSemanticElementRect(button).Width > before.Width + 1, timeoutMS: 5000, message: "The semantic rectangle never followed the ancestor scale.");

			var after = GetRequiredSemanticElementRect(button);

			Assert.AreEqual(before.Width * 2, after.Width, 2, "Width must be scaled by the ancestor ScaleX.");
			Assert.AreEqual(before.Height * 2, after.Height, 2, "Height must be scaled by the ancestor ScaleY.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Text_Controls_Are_Loaded_Then_Each_Owns_Its_Semantic_Node()
	{
		var autoSuggestBox = new AutoSuggestBox { Width = 200 };
		var textBox = new TextBox { Width = 200 };
		var passwordBox = new PasswordBox { Width = 200 };
		var comboBox = new ComboBox { Width = 200, ItemsSource = new[] { "a", "b" } };
		var root = new StackPanel { Children = { autoSuggestBox, textBox, passwordBox, comboBox } };

		try
		{
			await UITestHelper.Load(root);

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(autoSuggestBox)
					&& SemanticElementExists(textBox)
					&& SemanticElementExists(passwordBox)
					&& SemanticElementExists(comboBox),
				timeoutMS: 5000,
				message: "Timed out waiting for the semantic elements to be created.");
			await UITestHelper.WaitForIdle();

			var innerTextBox = FindFirstTextBox(autoSuggestBox);
			Assert.IsNotNull(innerTextBox, "The AutoSuggestBox template is expected to contain a TextBox.");
			Assert.IsTrue(SemanticElementExists(innerTextBox), "The templated TextBox must keep its own semantic node rather than borrowing the AutoSuggestBox one.");
			Assert.AreNotEqual(GetSemanticElementId(autoSuggestBox), GetSemanticElementId(innerTextBox), "The templated TextBox must not be collapsed onto the AutoSuggestBox node.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static Button CreateNamedButton(string name, double? width = null, double? height = null, Thickness? margin = null)
	{
		var button = new Button
		{
			Content = name,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
		};
		AutomationProperties.SetName(button, name);

		if (width is { } w)
		{
			button.Width = w;
		}

		if (height is { } h)
		{
			button.Height = h;
		}

		if (margin is { } m)
		{
			button.Margin = m;
		}

		return button;
	}

	private static TextBox? FindFirstTextBox(DependencyObject root)
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is TextBox textBox)
			{
				return textBox;
			}

			if (FindFirstTextBox(child) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	private static Rect GetRequiredSemanticElementRect(UIElement element)
	{
		var rect = GetSemanticElementRect(element);
		Assert.IsNotNull(rect, $"The semantic node {GetSemanticElementId(element)} for {element.GetType().Name} is missing.");
		return rect.Value;
	}

	private static object GetWebAssemblyAccessibilityInstance()
	{
		var type = Type.GetType("Uno.UI.Runtime.Skia.WebAssemblyAccessibility, Uno.UI.Runtime.Skia.WebAssembly.Browser");
		Assert.IsNotNull(type);
		var instanceProperty = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.IsNotNull(instanceProperty);
		var instance = instanceProperty.GetValue(null);
		Assert.IsNotNull(instance);
		return instance;
	}

	private static bool HasRegisteredSemanticElement(UIElement element)
	{
		var instance = GetWebAssemblyAccessibilityInstance();
		var hasSemanticElement = instance.GetType().GetMethod("HasSemanticElement", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.IsNotNull(hasSemanticElement);
		return (bool)hasSemanticElement.Invoke(instance, new object[] { element.Visual.Handle })!;
	}

	private static async Task WaitForComboBoxGeometryNodes(ComboBox comboBox)
	{
		await UITestHelper.WaitFor(
			() => comboBox.IsDropDownOpen
				&& comboBox.ItemsPanelRoot is { ActualWidth: > 0, ActualHeight: > 0 } itemsHost
				&& SemanticElementExists(itemsHost)
				&& Enumerable.Range(0, comboBox.Items.Count).All(index =>
					comboBox.ContainerFromIndex(index) is FrameworkElement { ActualWidth: > 0, ActualHeight: > 0 } item
					&& SemanticElementExists(item)),
			timeoutMS: 5000,
			message: "Timed out waiting for the open dropdown and all realized option nodes.");
		await UITestHelper.WaitForIdle();
	}

	private static async Task WaitForRepeaterGeometryNodes(ItemsRepeater repeater)
	{
		await UITestHelper.WaitFor(
			() => SemanticElementExists(repeater)
				&& Enumerable.Range(0, repeater.ItemsSourceView.Count).All(index =>
				repeater.TryGetElement(index) is FrameworkElement { ActualWidth: > 0, ActualHeight: > 0 } element
				&& SemanticElementExists(element)),
			timeoutMS: 5000,
			message: "Timed out waiting for all repeater item geometry nodes.");
		await UITestHelper.WaitForIdle();
	}

	private static object GetRepeaterSemanticRegion(ItemsRepeater repeater)
	{
		var instance = GetWebAssemblyAccessibilityInstance();
		var regionsField = instance.GetType().GetField("_virtualizedRegions", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.IsNotNull(regionsField);
		var regions = (System.Collections.IEnumerable)regionsField.GetValue(instance)!;
		return regions.Cast<object>().Single(candidate =>
			(IntPtr)candidate.GetType().GetProperty("ContainerHandle", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(candidate)! == repeater.Visual.Handle);
	}

	private static (Dictionary<int, IntPtr> ByIndex, Dictionary<IntPtr, int> ByHandle) ReadRepeaterRegionMaps(ItemsRepeater repeater)
	{
		var region = GetRepeaterSemanticRegion(repeater);
		var byIndexField = region.GetType().GetField("_realizedHandles", BindingFlags.Instance | BindingFlags.NonPublic);
		var byHandleField = region.GetType().GetField("_realizedIndices", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.IsNotNull(byIndexField);
		Assert.IsNotNull(byHandleField);
		return (
			new Dictionary<int, IntPtr>((Dictionary<int, IntPtr>)byIndexField.GetValue(region)!),
			new Dictionary<IntPtr, int>((Dictionary<IntPtr, int>)byHandleField.GetValue(region)!));
	}

	private static void AssertRepeaterRegionMaps(ItemsRepeater repeater)
	{
		var maps = ReadRepeaterRegionMaps(repeater);
		var realized = repeater.Children.Where(element => ItemsRepeater.GetVirtualizationInfo(element).IsRealized).ToArray();
		Assert.AreEqual(realized.Length, maps.ByIndex.Count, "The index map must contain exactly the realized containers.");
		Assert.AreEqual(realized.Length, maps.ByHandle.Count, "The reverse map must contain exactly the realized containers.");
		foreach (var element in realized)
		{
			var index = ItemsRepeater.GetVirtualizationInfo(element).Index;
			Assert.AreEqual(element.Visual.Handle, maps.ByIndex[index]);
			Assert.AreEqual(index, maps.ByHandle[element.Visual.Handle]);
			Assert.IsTrue(HasRegisteredSemanticElement(element));
			Assert.AreEqual(GetSemanticElementId(repeater), GetSemanticParentId(element));
			AssertSemanticRectMatchesLayout((FrameworkElement)element);
		}
	}

	private static void AssertComboBoxDropDownGeometry(ComboBox comboBox, string stage)
	{
		var itemsHost = comboBox.ItemsPanelRoot;
		Assert.IsNotNull(itemsHost);
		var hostBounds = GetLayoutRect(itemsHost);
		var hostRect = GetRequiredSemanticElementRect(itemsHost);
		Console.WriteLine($"ComboBox geometry ({stage}): host layout={hostBounds}, DOM={hostRect}");
		Assert.AreEqual(GetSemanticElementId(itemsHost), GetSemanticAttribute(comboBox, "aria-controls"));
		Assert.AreEqual("listbox", GetSemanticAttribute(itemsHost, "role"));
		AssertSemanticRectMatchesLayout(itemsHost);

		for (var index = 0; index < comboBox.Items.Count; index++)
		{
			var item = comboBox.ContainerFromIndex(index) as FrameworkElement;
			Assert.IsNotNull(item, $"Option {index} must remain realized during {stage}.");
			Assert.AreEqual(GetSemanticElementId(itemsHost), GetSemanticParentId(item), $"Option {index} has the wrong DOM parent during {stage}.");
			Assert.AreEqual("option", GetSemanticAttribute(item, "role"));
			AssertSemanticRectMatchesLayout(item);

			var bounds = GetLayoutRect(item);
			var rect = GetRequiredSemanticElementRect(item);
			Assert.AreEqual(bounds.X - hostBounds.X, rect.X - hostRect.X, Tolerance, $"Option {index} must use the listbox bbox origin on X during {stage}.");
			Assert.AreEqual(bounds.Y - hostBounds.Y, rect.Y - hostRect.Y, Tolerance, $"Option {index} must use the listbox bbox origin on Y during {stage}.");
		}
	}

	private static void StartSemanticGeometryCounts(IReadOnlyList<FrameworkElement> elements)
	{
		var ids = string.Join(",", elements.Select(element => $"'{GetSemanticElementId(element)}'"));
		// Observe actual DOM positioning writes even when the runtime has cached the imported JS function.
		InvokeBrowserJs($$"""
			(() => {
				const entries = globalThis.__unoGeometryPositionCounts = [];
				for (const id of [{{ids}}]) {
					const element = document.getElementById(id);
					if (!element) { throw new Error(`Missing semantic node ${id}`); }
					const style = element.style;
					const entry = { element, descriptor: Object.getOwnPropertyDescriptor(element, 'style'), count: 0 };
					entries.push(entry);
					const observed = new Proxy(style, {
						get(target, property) {
							const value = Reflect.get(target, property, target);
							return typeof value === 'function' ? value.bind(target) : value;
						},
						set(target, property, value) {
							if (property === 'width') { entry.count++; }
							return Reflect.set(target, property, value, target);
						}
					});
					Object.defineProperty(element, 'style', { configurable: true, get: () => observed });
				}
				return 'ok';
			})()
			""");
	}

	private static int[] ReadSemanticGeometryCounts()
		=> InvokeBrowserJs("globalThis.__unoGeometryPositionCounts.map(entry => entry.count).join(',')")
			.Split(',')
			.Select(value => int.Parse(value, CultureInfo.InvariantCulture))
			.ToArray();

	private static void StopSemanticGeometryCounts()
		=> InvokeBrowserJs("""
			(() => {
				for (const entry of globalThis.__unoGeometryPositionCounts || []) {
					if (entry.descriptor) {
						Object.defineProperty(entry.element, 'style', entry.descriptor);
					} else {
						delete entry.element.style;
					}
				}
				delete globalThis.__unoGeometryPositionCounts;
				return 'ok';
			})()
			""");

	private static void AssertSemanticRectMatchesLayout(FrameworkElement element)
	{
		var id = GetSemanticElementId(element);
		var rect = GetRequiredSemanticElementRect(element);
		var expected = GetLayoutRect(element);

		Assert.AreEqual(expected.X, rect.X, Tolerance, $"{id}: x mismatch (semantic rect {rect}).");
		Assert.AreEqual(expected.Y, rect.Y, Tolerance, $"{id}: y mismatch (semantic rect {rect}).");
		Assert.AreEqual(expected.Width, rect.Width, Tolerance, $"{id}: width mismatch (semantic rect {rect}).");
		Assert.AreEqual(expected.Height, rect.Height, Tolerance, $"{id}: height mismatch (semantic rect {rect}).");
	}

	private static Rect GetLayoutRect(FrameworkElement element)
		=> element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

	private static void AssertSemanticRectChangeMatchesLayout(FrameworkElement element, Rect before, Rect beforeLayout)
	{
		var after = GetRequiredSemanticElementRect(element);
		var afterLayout = GetLayoutRect(element);

		// Native input border/padding is constant; compare deltas to isolate the emitted geometry.
		Assert.AreEqual(afterLayout.X - beforeLayout.X, after.X - before.X, Tolerance, "The semantic X change must match the transformed owner.");
		Assert.AreEqual(afterLayout.Y - beforeLayout.Y, after.Y - before.Y, Tolerance, "The semantic Y change must match the transformed owner.");
		Assert.AreEqual(afterLayout.Width - beforeLayout.Width, after.Width - before.Width, Tolerance, "The semantic width change must match the transformed owner.");
		Assert.AreEqual(afterLayout.Height - beforeLayout.Height, after.Height - before.Height, Tolerance, "The semantic height change must match the transformed owner.");
	}
#endif
}
