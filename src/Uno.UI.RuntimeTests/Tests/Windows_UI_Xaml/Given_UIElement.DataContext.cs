using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

partial class Given_UIElement
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Subtree_SeveredFromDataContextSource()
	{
		const int DC = 312;

		var nested2 = new Border() { Name = "nested2" };
		var nested1 = new Border() { Name = "nested1", Child = nested2 };
		var host = new Border() { Name = "host", Child = nested1 };
		await UITestHelper.Load(host, x => x.IsLoaded);

		host.DataContext = DC;
		Assert.AreEqual(DC, nested1.DataContext, "1. initially, DC (nested1) should be inherited");
		Assert.AreEqual(DC, nested2.DataContext, "1. initially, DC (nested2) should be inherited");

		host.Child = null;
		Assert.IsNull(nested1.DataContext, "2. when detached, inherited DC(nested1) should be cleared");
		Assert.IsNull(nested2.DataContext, "2. when detached, inherited DC(nested2) should be cleared");

		host.Child = nested1;
		Assert.AreEqual(DC, nested1.DataContext, "3. when reattached, DC (nested1) should be inherited again");
		Assert.AreEqual(DC, nested2.DataContext, "3. when reattached, DC (nested2) should be inherited again");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_SeveredSubtree_ContainsDataContextSource()
	{
		const int DC = 312;

		//   v detachment point
		// H > N1 > N2 > N3 > N4
		//          ^ DC owner, and propagation source
		//               ^  + ^ inherited DC
		var nested4 = new Border() { Name = "nested4", };
		var nested3 = new Border() { Name = "nested3", Child = nested4 };
		var nested2 = new Border() { Name = "nested2", Child = nested3 };
		var nested1 = new Border() { Name = "nested1", Child = nested2 };
		var host = new Border() { Name = "host", Child = nested1 };
		await UITestHelper.Load(host, x => x.IsLoaded);

		nested2.DataContext = DC;
		Assert.AreEqual(DC, nested3.DataContext, "1. initially, DC (nested3) should be inherited");
		Assert.AreEqual(DC, nested4.DataContext, "1. initially, DC (nested4) should be inherited");

		host.Child = null;
		Assert.AreEqual(DC, nested3.DataContext, "2. when detached, DC (nested3) should still be inherited&unaffected");
		Assert.AreEqual(DC, nested4.DataContext, "2. when detached, DC (nested4) should still be inherited&unaffected");

		host.Child = nested1;
		Assert.AreEqual(DC, nested3.DataContext, "3. when reattached, DC (nested3) should still be inherited&unaffected");
		Assert.AreEqual(DC, nested4.DataContext, "3. when reattached, DC (nested4) should still be inherited&unaffected");
	}

#if HAS_UNO // there is no DC on non-FE DO for winui, not directly*
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task SingleParentNonFE_Direct_DataContext_Propagation_Works()
	{
		var dc = new { Data = "Context" };

		var run = new Run();
		run.SetBinding(Run.TextProperty, new Binding { Path = new(nameof(dc.Data)) });
		var tblock = new TextBlock();
		tblock.Inlines.Add(run);
		tblock.DataContext = dc;

		await UITestHelper.Load(tblock, x => x.IsLoaded);

		Assert.AreEqual(dc, run.DataContext);
		Assert.AreEqual(dc.Data, run.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task MultiParentNonFE_Direct_DataContext_Propagation_WorksOnlyOnce1()
	{
		var brush = new SolidColorBrush(Colors.SkyBlue);

#if DEBUG
		using var disp = brush.RegisterDisposablePropertyChangedCallback(
			SolidColorBrush.DataContextProperty,
			(s, e) => { /* breakpoint here to investigate */ });
#endif

		// variant: assignment order: foreground > dc

		var setup0 = new Control();
		setup0.Foreground = brush;
		setup0.DataContext = new { Data = "Context 0" };
		Assert.IsNotNull(brush.DataContext, "0. until it is attached to multiple \"parent\", dc propagate should work");

		var setup1 = new Control();
		setup1.Foreground = brush;
		setup1.DataContext = new { Data = "Context 1" };
		Assert.IsNull(brush.DataContext, "1. once it is attached to multiple \"parent\", dc should no longer propagate");

		setup0.Foreground = null;
		setup1.Foreground = null;
		var setup2 = new Control();
		setup2.Foreground = brush;
		setup2.DataContext = new { Data = "Context 2" };
		Assert.IsNull(brush.DataContext, "2. once it has been attached to multiple \"parent\", dc shouldn't propagate anymore even if we only have a single parent now");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task MultiParentNonFE_Direct_DataContext_Propagation_WorksOnlyOnce2()
	{
		var brush = new SolidColorBrush(Colors.SkyBlue);

#if DEBUG
		using var disp = brush.RegisterDisposablePropertyChangedCallback(
			SolidColorBrush.DataContextProperty,
			(s, e) => { /* breakpoint here to investigate */ });
#endif

		// variant: assignment order: dc > foreground

		var setup0 = new Control();
		setup0.DataContext = new { Data = "Context 0" };
		setup0.Foreground = brush;
		Assert.IsNotNull(brush.DataContext, "0. until it is attached to multiple \"parent\", dc propagate should work");

		var setup1 = new Control();
		setup1.DataContext = new { Data = "Context 1" };
		setup1.Foreground = brush;
		Assert.IsNull(brush.DataContext, "1. once it is attached to multiple \"parent\", dc should no longer propagate");

		setup0.Foreground = null;
		setup1.Foreground = null;
		var setup2 = new Control();
		setup2.DataContext = new { Data = "Context 2" };
		setup2.Foreground = brush;
		Assert.IsNull(brush.DataContext, "2. once it has been attached to multiple \"parent\", dc shouldn't propagate anymore even if we only have a single parent now");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task MultiParentNonFE_Inherited_DataContext_Propagation_WorksOnlyOnce()
	{
		// all permutations just in case
		var variants = """
			A. child.fg > host.dc > host.child
			B. child.fg > host.child > host.dc
			C. host.dc > child.fg > host.child
			D. host.dc > host.child > child.fg
			E. host.child > host.dc > child.fg
			F. host.child > child.fg > host.dc
		""".Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
			.Where(x => !x.StartsWith("//"))
			.Select(x => new
			{
				Label = x[0..1],
				Instructions = x[3..].Split(" > "),
			})
			.ToArray();
		var instructionMap = new Dictionary<string, Action<Border, Control, object, Brush>>
		{
			["child.fg"] = (host, child, dc, brush) => child.Foreground = brush,
			["host.dc"] = (host, child, dc, brush) => host.DataContext = dc,
			["host.child"] = (host, child, dc, brush) => host.Child = child,
		};

		foreach (var variant in variants)
		{
			var brush = new SolidColorBrush(Colors.SkyBlue);
#if DEBUG
			using var disp = brush.RegisterDisposablePropertyChangedCallback(
				SolidColorBrush.DataContextProperty,
				(s, e) => { /* breakpoint here to investigate */ });
#endif

			var setup0 = new
			{
				Host = new Border(),
				Child = new Control(),
				DC = new { Data = $"Context {variant.Label}0" },
			};
			instructionMap[variant.Instructions[0]](setup0.Host, setup0.Child, setup0.DC, brush);
			instructionMap[variant.Instructions[1]](setup0.Host, setup0.Child, setup0.DC, brush);
			instructionMap[variant.Instructions[2]](setup0.Host, setup0.Child, setup0.DC, brush);
			Assert.IsNotNull(brush.DataContext, $"{variant.Label}0. until it is attached to multiple \"parent\", dc propagate should work");

			var setup1 = new
			{
				Host = new Border(),
				Child = new Control(),
				DC = new { Data = $"Context {variant.Label}1" },
			};
			instructionMap[variant.Instructions[0]](setup1.Host, setup1.Child, setup1.DC, brush);
			instructionMap[variant.Instructions[1]](setup1.Host, setup1.Child, setup1.DC, brush);
			instructionMap[variant.Instructions[2]](setup1.Host, setup1.Child, setup1.DC, brush);
			Assert.IsNull(brush.DataContext, $"{variant.Label}1. once it is attached to multiple \"parent\", dc should no longer propagate");

			setup0.Child.Foreground = null;
			setup1.Child.Foreground = null;
			var setup2 = new
			{
				Host = new Border(),
				Child = new Control(),
				DC = new { Data = $"Context {variant.Label}2" },
			};
			instructionMap[variant.Instructions[0]](setup2.Host, setup2.Child, setup2.DC, brush);
			instructionMap[variant.Instructions[1]](setup2.Host, setup2.Child, setup2.DC, brush);
			instructionMap[variant.Instructions[2]](setup2.Host, setup2.Child, setup2.DC, brush);
			Assert.IsNull(brush.DataContext, $"{variant.Label}2. once it has been attached to multiple \"parent\", dc shouldn't propagate anymore even if we only have a single parent now");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task SingleParentNonFE_Parent_Collected_Then_Next_Parent_Counts_As_Second()
	{
		var brush = new SolidColorBrush(Colors.SkyBlue);
		BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding { Path = new("Color") });

		// The first owner's DataContext stays alive (held here); only the owner itself gets collected.
		var firstDataContext = new { Color = Colors.Red };
		var firstOwnerRef = AttachToOwner(brush, firstDataContext);
		Assert.AreEqual(Colors.Red, brush.Color, "0. a single parent propagates its DataContext");

		Assert.IsTrue(await TestHelper.TryWaitUntilCollected(firstOwnerRef), "Pre-condition: the first owner must be collectible while the brush is alive");

		// A second owner with no DataContext. It already has a local Background, so the assignment is a
		// same-precedence replacement: nothing but the association itself touches the brush's inherited DataContext.
		var secondOwner = new Border { Background = new SolidColorBrush(Colors.Yellow) };
		secondOwner.Background = brush;
		Assert.AreNotEqual(Colors.Red, brush.Color, "1. a dead parent's DataContext must not survive re-association");
		Assert.AreEqual((Windows.UI.Color)SolidColorBrush.ColorProperty.GetMetadata(typeof(SolidColorBrush)).DefaultValue, brush.Color, "1. with no DataContext the bound property falls back to its default");

		// The collected parent still counts: the brush has had two parents, so inheritance is off for good, exactly as
		// when the first parent is still alive (MultiParentNonFE_*). The outcome must not depend on GC timing.
		// Known WinUI divergence: a destroyed WinUI parent removes itself from its values (ReleaseImpl ->
		// ResetReferencesFromChildren -> RemoveParent), so the next consumer is a single parent again. Uno has no such
		// hook, so a collected parent keeps counting; only an explicit removal or the ALC sweep frees the slot.
		secondOwner.DataContext = new { Color = Colors.Green };
		Assert.AreNotEqual(Colors.Green, brush.Color, "2. once it has been attached to multiple \"parent\", dc should no longer propagate");

		GC.KeepAlive(firstDataContext);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public void SingleParentNonFE_Parent_Removes_Value_Then_Next_Parent_Starts_Clean()
	{
		var brush = new SolidColorBrush(Colors.SkyBlue);
		BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding { Path = new("Color") });

		var firstOwner = new Border { DataContext = new { Color = Colors.Red } };
		firstOwner.Background = brush;
		Assert.AreEqual(Colors.Red, brush.Color, "0. a single parent propagates its DataContext");

		// Losing the only parent removes the inheritance context: bindings re-resolve against nothing.
		firstOwner.Background = null;
		Assert.AreNotEqual(Colors.Red, brush.Color, "1. the removed parent's DataContext must not stay applied");

		// Same-precedence replacement on an owner with no DataContext: nothing but the association touches the brush.
		var secondOwner = new Border { Background = new SolidColorBrush(Colors.Yellow) };
		secondOwner.Background = brush;
		Assert.AreEqual((Windows.UI.Color)SolidColorBrush.ColorProperty.GetMetadata(typeof(SolidColorBrush)).DefaultValue, brush.Color, "2. with no DataContext the bound property falls back to its default");

		secondOwner.DataContext = new { Color = Colors.Green };
		Assert.AreEqual(Colors.Green, brush.Color, "3. the new single parent's DataContext propagates");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task SharedNonFE_ObjectBinding_Subtree_Severed_Then_Bound_Value_Released()
	{
		// A shared non-FE value whose object-typed property copies the DataContext itself: once the owner's subtree
		// leaves the tree (and so loses its inherited DataContext), the copy must be re-resolved to null so the
		// shared object does not retain the view model.
		var trigger = new ObjectValueTrigger();
		BindingOperations.SetBinding(trigger, ObjectValueTrigger.ValueProperty, new Binding());

		var root = new ContentControl();
		await UITestHelper.Load(root, x => x.IsLoaded);

		try
		{
			var viewModelRef = AttachSubtreeWithTrigger(root, trigger);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(viewModelRef.IsAlive);
			Assert.IsNotNull(trigger.Value, "0. the inherited DataContext is copied into the bound object property");

			root.Content = null;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsNull(trigger.Value, "1. severing the subtree re-resolves the binding against the lost inheritance context");

			root.DataContext = null;
			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(viewModelRef), "2. nothing retains the view model once the subtree is gone");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}

		GC.KeepAlive(trigger);
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static WeakReference AttachSubtreeWithTrigger(ContentControl root, ObjectValueTrigger trigger)
	{
		var viewModel = new object();
		root.DataContext = viewModel;

		// The element owning the shared trigger sits below the severed root, so its DataContext is inherited.
		var page = new Border { Child = new Border { Tag = trigger } };
		root.Content = page;

		return new WeakReference(viewModel);
	}

	private sealed partial class ObjectValueTrigger : StateTriggerBase
	{
		public static DependencyProperty ValueProperty { get; } = DependencyProperty.Register(
			nameof(Value), typeof(object), typeof(ObjectValueTrigger), new PropertyMetadata(null));

		public object Value
		{
			get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static WeakReference AttachToOwner(Brush brush, object dataContext)
	{
		var owner = new Border { DataContext = dataContext };
		owner.Background = brush;
		return new WeakReference(owner);
	}

	// An object that isn't a UIElement holds the DataContext its element pushes into it weakly; the element owns it. The tests
	// below force collections and check that such objects and their bindings still resolve to the right view model, never to
	// the weak handle or to nothing, and that a DataContext set on the object itself is still owned by it.

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task NonFE_Inherited_DataContext_Survives_GC_While_Owner_Alive()
	{
		var brush = new SolidColorBrush(Colors.SkyBlue);
		BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding { Path = new(nameof(DataContextModel.Color)) });

		var row = new RowDefinition();
		BindingOperations.SetBinding(row, RowDefinition.HeightProperty, new Binding { Path = new(nameof(DataContextModel.RowHeight)) });

		var run = new Run();
		BindingOperations.SetBinding(run, Run.TextProperty, new Binding { Path = new(nameof(DataContextModel.Text)) });

		var trigger = new StateTrigger();
		BindingOperations.SetBinding(trigger, StateTrigger.IsActiveProperty, new Binding { Path = new(nameof(DataContextModel.IsActive)) });

		var textBlock = new TextBlock();
		textBlock.Inlines.Add(run);
		var owner = new Grid { Width = 100, Height = 100 };
		owner.RowDefinitions.Add(row);
		owner.Children.Add(new Border { Background = brush });
		owner.Children.Add(textBlock);
		VisualStateManager.SetVisualStateGroups(owner, new List<VisualStateGroup> { new() { States = { new VisualState { StateTriggers = { trigger } } } } });

		SetOwnedDataContext(owner, Colors.Red, rowHeight: 40, text: "first", isActive: true);
		await UITestHelper.Load(owner, x => x.IsLoaded);
		await CollectAsync();

		AssertBound("1. after a collection", Colors.Red, 40, "first", true);
		Assert.AreSame(owner.DataContext, brush.DataContext, "1. the brush still resolves its owner's DataContext");
		Assert.AreSame(owner.DataContext, row.DataContext, "1. the row still resolves its owner's DataContext");
		Assert.AreSame(owner.DataContext, run.DataContext, "1. the run still resolves its owner's DataContext");
		Assert.AreSame(owner.DataContext, trigger.DataContext, "1. the trigger still resolves its owner's DataContext");

		UpdateOwnedDataContext(owner, Colors.Green, rowHeight: 30, text: "updated", isActive: false);
		await CollectAsync();
		AssertBound("2. after the view model changed", Colors.Green, 30, "updated", false);

		// A null DataContext reaches every object below the owner, and the next one replaces it.
		owner.DataContext = null;
		await CollectAsync();
		Assert.IsNull(brush.DataContext, "3. the brush follows its owner's null DataContext");
		Assert.IsNull(trigger.DataContext, "3. the trigger follows its owner's null DataContext");

		SetOwnedDataContext(owner, Colors.Blue, rowHeight: 20, text: "second", isActive: true);
		await CollectAsync();
		AssertBound("4. after a new DataContext", Colors.Blue, 20, "second", true);
		Assert.AreSame(owner.DataContext, brush.DataContext, "4. the brush resolves its owner's new DataContext");

		UpdateOwnedDataContext(owner, Colors.Yellow, rowHeight: 10, text: "updated again", isActive: false);
		await CollectAsync();
		AssertBound("5. after the new view model changed", Colors.Yellow, 10, "updated again", false);

		void AssertBound(string step, Windows.UI.Color color, double rowHeight, string text, bool isActive)
		{
			Assert.AreEqual(color, brush.Color, $"{step}: SolidColorBrush.Color");
			Assert.AreEqual(rowHeight, row.Height.Value, $"{step}: RowDefinition.Height");
			Assert.AreEqual(text, run.Text, $"{step}: Run.Text");
			Assert.AreEqual(isActive, trigger.IsActive, $"{step}: StateTrigger.IsActive");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task NonFE_Binding_Added_After_DataContext_Pushed_Then_Resolves()
	{
		var brush = new SolidColorBrush(Colors.SkyBlue);
		var owner = new Border { Width = 10, Height = 10, Background = brush };
		SetOwnedDataContext(owner, Colors.Red);
		await UITestHelper.Load(owner, x => x.IsLoaded);
		await CollectAsync();

		// A new binding starts from the DataContext read back from the brush.
		BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding { Path = new(nameof(DataContextModel.Color)) });
		Assert.AreEqual(Colors.Red, brush.Color, "1. the binding resolves against the inherited DataContext");

		UpdateOwnedDataContext(owner, Colors.Green);
		await CollectAsync();
		Assert.AreEqual(Colors.Green, brush.Color, "2. the binding follows the view model");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task NonFE_DataContext_Bound_To_Sub_Then_Resolves_And_Updates()
	{
		var brush = new SolidColorBrush(Colors.SkyBlue);
		BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding { Path = new(nameof(DataContextModel.Color)) });
		var owner = new Border { Width = 10, Height = 10, Background = brush };
		SetOwnedDataContext(owner, Colors.Yellow, sub: Colors.Red);
		await UITestHelper.Load(owner, x => x.IsLoaded);
		await CollectAsync();

		// DataContext="{Binding Sub}" on the brush, added after the owner pushed its DataContext: the binding starts from
		// the inherited DataContext, and the bound Sub becomes the brush's own (local) DataContext.
		BindingOperations.SetBinding(brush, SolidColorBrush.DataContextProperty, new Binding { Path = new(nameof(DataContextModel.Sub)) });
		Assert.AreEqual(Colors.Red, brush.Color, "1. bound against the sub view model");

		await CollectAsync();
		ReplaceOwnedSub(owner, Colors.Green);
		await CollectAsync();
		Assert.AreEqual(Colors.Green, brush.Color, "2. a new sub view model is picked up");

		SetOwnedDataContext(owner, Colors.Yellow, sub: Colors.Blue);
		await CollectAsync();
		Assert.AreEqual(Colors.Blue, brush.Color, "3. a new owner DataContext is picked up");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task NonFE_Local_DataContext_Stays_Owned()
	{
		// Only the inherited DataContext is non-owning: a DataContext set on the object itself is kept.
		var brush = new SolidColorBrush(Colors.SkyBlue);
		var flyout = new Flyout();
		var brushModelRef = SetLocalDataContext(brush);
		var flyoutModelRef = SetLocalDataContext(flyout);

		var owner = new Border { Width = 10, Height = 10, Background = brush };
		Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.SetAttachedFlyout(owner, flyout);
		SetOwnedDataContext(owner, Colors.Red);
		await UITestHelper.Load(owner, x => x.IsLoaded);
		await CollectAsync();

		Assert.IsTrue(brushModelRef.IsAlive, "The brush's local DataContext must not be collected");
		Assert.AreSame(brushModelRef.Target, brush.DataContext, "The brush's local DataContext wins over the inherited one");
		Assert.IsTrue(flyoutModelRef.IsAlive, "The flyout's local DataContext must not be collected");
		Assert.AreSame(flyoutModelRef.Target, flyout.DataContext, "The flyout keeps its local DataContext");
	}

	private static async Task CollectAsync()
	{
		for (var i = 0; i < 2; i++)
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);
			await Task.Yield();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	// The view models are created and changed in non-inlined frames, so only the owner element references them.

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static void SetOwnedDataContext(
		FrameworkElement owner, Windows.UI.Color color, double rowHeight = 0, string text = null, bool isActive = false, Windows.UI.Color? sub = null)
		=> owner.DataContext = new DataContextModel
		{
			Color = color,
			RowHeight = new GridLength(rowHeight),
			Text = text,
			IsActive = isActive,
			Sub = sub is { } subColor ? new DataContextModel { Color = subColor } : null,
		};

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static void UpdateOwnedDataContext(FrameworkElement owner, Windows.UI.Color color, double rowHeight = 0, string text = null, bool isActive = false)
	{
		var model = (DataContextModel)owner.DataContext;
		model.Color = color;
		model.RowHeight = new GridLength(rowHeight);
		model.Text = text;
		model.IsActive = isActive;
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static void ReplaceOwnedSub(FrameworkElement owner, Windows.UI.Color subColor)
		=> ((DataContextModel)owner.DataContext).Sub = new DataContextModel { Color = subColor };

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static WeakReference SetLocalDataContext(DependencyObject target)
	{
		var model = new DataContextModel();
		switch (target)
		{
			case Brush brush:
				brush.DataContext = model;
				break;
			case Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase flyout:
				flyout.DataContext = model;
				break;
			default:
				throw new ArgumentException($"Unsupported target {target.GetType()}", nameof(target));
		}

		return new WeakReference(model);
	}

	private sealed class DataContextModel : System.ComponentModel.INotifyPropertyChanged
	{
		private Windows.UI.Color _color;
		private GridLength _rowHeight;
		private string _text;
		private bool _isActive;
		private DataContextModel _sub;

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		public Windows.UI.Color Color { get => _color; set => Set(ref _color, value); }

		public GridLength RowHeight { get => _rowHeight; set => Set(ref _rowHeight, value); }

		public string Text { get => _text; set => Set(ref _text, value); }

		public bool IsActive { get => _isActive; set => Set(ref _isActive, value); }

		public DataContextModel Sub { get => _sub; set => Set(ref _sub, value); }

		private void Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
		{
			field = value;
			PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
		}
	}
#endif
}
