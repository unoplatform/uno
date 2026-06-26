using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

// BC58 parity guard: DataContext is a FrameworkElement-only *public* property, but {Binding} must still resolve
// on non-FrameworkElement DependencyObjects that are connected to an FE tree. These four cases were confirmed to
// resolve on native WinUI (Setter.Value binding does NOT resolve in WinUI and is intentionally not covered).
[TestClass]
public class Given_NonFE_DataContextBinding
{
	private const string Ns = "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'";

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ColumnDefinition_Width_Bound()
	{
		var grid = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			$"<Grid {Ns}><Grid.ColumnDefinitions><ColumnDefinition Width='{{Binding ColWidth}}' /></Grid.ColumnDefinitions><TextBlock Text='x' /></Grid>");
		grid.DataContext = new Vm();
		await UITestHelper.Load(grid, x => x.IsLoaded);

		Assert.AreEqual(123d, grid.ColumnDefinitions[0].Width.Value, "ColumnDefinition.Width should resolve {Binding} from the Grid's DataContext");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RowDefinition_Height_Bound()
	{
		var grid = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			$"<Grid {Ns}><Grid.RowDefinitions><RowDefinition Height='{{Binding RowHeight}}' /></Grid.RowDefinitions><TextBlock Text='x' /></Grid>");
		grid.DataContext = new Vm();
		await UITestHelper.Load(grid, x => x.IsLoaded);

		Assert.AreEqual(45d, grid.RowDefinitions[0].Height.Value, "RowDefinition.Height should resolve {Binding} from the Grid's DataContext");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Run_Text_Bound()
	{
		var tb = (TextBlock)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			$"<TextBlock {Ns}><Run Text='{{Binding Msg}}' /></TextBlock>");
		tb.DataContext = new Vm();
		await UITestHelper.Load(tb, x => x.IsLoaded);

		Assert.AreEqual("hello", ((Run)tb.Inlines[0]).Text, "Run.Text should resolve {Binding} from the TextBlock's DataContext");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_StateTrigger_IsActive_Bound()
	{
		var root = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			$"<Grid {Ns}><VisualStateManager.VisualStateGroups><VisualStateGroup><VisualState x:Name='S'><VisualState.StateTriggers><StateTrigger IsActive='{{Binding Flag}}' /></VisualState.StateTriggers><VisualState.Setters><Setter Target='tb.Opacity' Value='0.5' /></VisualState.Setters></VisualState></VisualStateGroup></VisualStateManager.VisualStateGroups><TextBlock x:Name='tb' Text='x' /></Grid>");
		root.DataContext = new Vm();
		await UITestHelper.Load(root, x => x.IsLoaded);

		var groups = VisualStateManager.GetVisualStateGroups(root);
		var trigger = (StateTrigger)groups[0].States[0].StateTriggers[0];
		Assert.IsTrue(trigger.IsActive, "StateTrigger.IsActive should resolve {Binding} from the ambient DataContext");
	}

	public sealed class Vm
	{
		public GridLength ColWidth { get; } = new GridLength(123);
		public GridLength RowHeight { get; } = new GridLength(45);
		public string Msg { get; } = "hello";
		public bool Flag { get; } = true;
	}
}
