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
using Windows.UI;

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

#if HAS_UNO // non-FE DependencyObjects have no DataContext of their own on WinUI; Uno keeps an inheritance-context
	// (mentor) so {Binding}s on them resolve. Since the DataContext itself is no longer observable on a non-FE object,
	// these tests observe propagation through a {Binding}: a bound property updating == the ambient DataContext reached it.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task SingleParentNonFE_Direct_DataContext_Propagation_Works()
	{
		var dc = new { Data = "Context" };

		var run = new Run();
		BindingOperations.SetBinding(run, Run.TextProperty, new Binding { Path = new(nameof(dc.Data)) });
		var tblock = new TextBlock();
		tblock.Inlines.Add(run);
		tblock.DataContext = dc;

		await UITestHelper.Load(tblock, x => x.IsLoaded);

		// Run is non-FE: its {Binding} resolves against the connected TextBlock's DataContext (inheritance-context).
		Assert.AreEqual(dc.Data, run.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task MultiParentNonFE_Direct_DataContext_Propagation_WorksOnlyOnce1()
	{
		var brush = new SolidColorBrush(Colors.SkyBlue);
		// Color tracks the ambient DataContext: it updates only while the brush's inheritance-context is live.
		BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding { Path = new("Color") });

		// variant: assignment order: foreground > dc

		var setup0 = new PlainControl();
		setup0.Foreground = brush;
		setup0.DataContext = new { Color = Colors.Red };
		Assert.AreEqual(Colors.Red, brush.Color, "0. until it is attached to multiple \"parent\", dc propagate should work");

		var setup1 = new PlainControl();
		setup1.Foreground = brush;
		setup1.DataContext = new { Color = Colors.Green };
		Assert.AreNotEqual(Colors.Green, brush.Color, "1. once it is attached to multiple \"parent\", dc should no longer propagate");

		setup0.Foreground = null;
		setup1.Foreground = null;
		var setup2 = new PlainControl();
		setup2.Foreground = brush;
		setup2.DataContext = new { Color = Colors.Blue };
		Assert.AreNotEqual(Colors.Blue, brush.Color, "2. once it has been attached to multiple \"parent\", dc shouldn't propagate anymore even if we only have a single parent now");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task MultiParentNonFE_Direct_DataContext_Propagation_WorksOnlyOnce2()
	{
		var brush = new SolidColorBrush(Colors.SkyBlue);
		BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding { Path = new("Color") });

		// variant: assignment order: dc > foreground

		var setup0 = new PlainControl();
		setup0.DataContext = new { Color = Colors.Red };
		setup0.Foreground = brush;
		Assert.AreEqual(Colors.Red, brush.Color, "0. until it is attached to multiple \"parent\", dc propagate should work");

		var setup1 = new PlainControl();
		setup1.DataContext = new { Color = Colors.Green };
		setup1.Foreground = brush;
		Assert.AreNotEqual(Colors.Green, brush.Color, "1. once it is attached to multiple \"parent\", dc should no longer propagate");

		setup0.Foreground = null;
		setup1.Foreground = null;
		var setup2 = new PlainControl();
		setup2.DataContext = new { Color = Colors.Blue };
		setup2.Foreground = brush;
		Assert.AreNotEqual(Colors.Blue, brush.Color, "2. once it has been attached to multiple \"parent\", dc shouldn't propagate anymore even if we only have a single parent now");
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
			// Color tracks the ambient DataContext: it updates only while the brush's inheritance-context is live.
			BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding { Path = new("Color") });

			var setup0 = new
			{
				Host = new Border(),
				Child = new PlainControl(),
				DC = (object)new { Color = Colors.Red },
			};
			instructionMap[variant.Instructions[0]](setup0.Host, setup0.Child, setup0.DC, brush);
			instructionMap[variant.Instructions[1]](setup0.Host, setup0.Child, setup0.DC, brush);
			instructionMap[variant.Instructions[2]](setup0.Host, setup0.Child, setup0.DC, brush);
			Assert.AreEqual(Colors.Red, brush.Color, $"{variant.Label}0. until it is attached to multiple \"parent\", dc propagate should work");

			var setup1 = new
			{
				Host = new Border(),
				Child = new PlainControl(),
				DC = (object)new { Color = Colors.Green },
			};
			instructionMap[variant.Instructions[0]](setup1.Host, setup1.Child, setup1.DC, brush);
			instructionMap[variant.Instructions[1]](setup1.Host, setup1.Child, setup1.DC, brush);
			instructionMap[variant.Instructions[2]](setup1.Host, setup1.Child, setup1.DC, brush);
			Assert.AreNotEqual(Colors.Green, brush.Color, $"{variant.Label}1. once it is attached to multiple \"parent\", dc should no longer propagate");

			setup0.Child.Foreground = null;
			setup1.Child.Foreground = null;
			var setup2 = new
			{
				Host = new Border(),
				Child = new PlainControl(),
				DC = (object)new { Color = Colors.Blue },
			};
			instructionMap[variant.Instructions[0]](setup2.Host, setup2.Child, setup2.DC, brush);
			instructionMap[variant.Instructions[1]](setup2.Host, setup2.Child, setup2.DC, brush);
			instructionMap[variant.Instructions[2]](setup2.Host, setup2.Child, setup2.DC, brush);
			Assert.AreNotEqual(Colors.Blue, brush.Color, $"{variant.Label}2. once it has been attached to multiple \"parent\", dc shouldn't propagate anymore even if we only have a single parent now");
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
		Assert.AreEqual((Color)SolidColorBrush.ColorProperty.GetMetadata(typeof(SolidColorBrush)).DefaultValue, brush.Color, "1. with no DataContext the bound property falls back to its default");

		// The collected parent still counts: the brush has had two parents, so inheritance is off for good, exactly as
		// when the first parent is still alive (MultiParentNonFE_*). The outcome must not depend on collection timing.
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
		Assert.AreEqual((Color)SolidColorBrush.ColorProperty.GetMetadata(typeof(SolidColorBrush)).DefaultValue, brush.Color, "2. with no DataContext the bound property falls back to its default");

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

	private sealed partial class PlainControl : Control
	{
	}
#endif
}
