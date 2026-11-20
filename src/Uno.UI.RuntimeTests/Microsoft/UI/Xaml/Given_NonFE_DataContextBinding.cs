#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

// WinUI parity guard: DataContext is a FrameworkElement-only *public* property, but {Binding} must still resolve
// on non-FrameworkElement DependencyObjects that are connected to an FE tree. These cases were confirmed on native
// WinUI, including that a Setter.Value binding resolves in a VisualState but not in a Style.
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
	[DataRow(true)]
	[DataRow(false)]
	public async Task When_StateTrigger_IsActive_Bound(bool flag)
	{
		var root = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			$"<Grid {Ns}><VisualStateManager.VisualStateGroups><VisualStateGroup><VisualState x:Name='S'><VisualState.StateTriggers><StateTrigger IsActive='{{Binding Flag}}' /></VisualState.StateTriggers><VisualState.Setters><Setter Target='tb.Opacity' Value='0.5' /></VisualState.Setters></VisualState></VisualStateGroup></VisualStateManager.VisualStateGroups><TextBlock x:Name='tb' Text='x' /></Grid>");
		root.DataContext = new Vm { Flag = flag };
		await UITestHelper.Load(root, x => x.IsLoaded);

		var groups = VisualStateManager.GetVisualStateGroups(root);
		var trigger = (StateTrigger)groups[0].States[0].StateTriggers[0];
		Assert.AreEqual(flag, trigger.IsActive, "StateTrigger.IsActive should resolve {Binding} from the ambient DataContext");
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26248")]
	public async Task When_VisualState_Setter_Bound_And_StateTrigger_Activates()
	{
		var page = await LoadSetterBindingPage(new VisualStateSetterBindingViewModel());

		Assert.AreEqual("flagged", page.TriggeredFirst.Text);
		Assert.AreEqual("other", page.TriggeredSecond.Text);
		Assert.AreEqual("state applied", page.TriggeredPlain.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26248")]
	public async Task When_VisualState_KeyFrame_Bound_And_StateTrigger_Activates()
	{
		var page = await LoadSetterBindingPage(new VisualStateSetterBindingViewModel());

		await TestServices.WindowHelper.WaitFor(() => page.KeyFrameLabel.Text == "flagged", message: $"Text was '{page.KeyFrameLabel.Text}'");
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26248")]
	public async Task When_VisualState_Setter_Bound_In_ControlTemplate_And_StateTrigger_Activates()
	{
		var page = await LoadSetterBindingPage(new VisualStateSetterBindingViewModel());

		Assert.AreEqual("flagged", FindTextBlock(page.InheritedHost, "TemplateLabel")?.Text, "DataContext inherited by the template");
		Assert.AreEqual("flagged", FindTextBlock(page.ContentHost, "TemplateLabel")?.Text, "DataContext from {TemplateBinding Content}");
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26248")]
	public async Task When_VisualState_Setters_Bound_And_GoToState()
	{
		var page = await LoadSetterBindingPage(new VisualStateSetterBindingViewModel());

		Assert.IsTrue(VisualStateManager.GoToState(page, "ManualA", false));
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("flagged", page.ManualFirst.Text);
		Assert.AreEqual("other", page.ManualSecond.Text, "Second bound setter of the state");

		Assert.IsTrue(VisualStateManager.GoToState(page, "ManualB", false));
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("other", page.ManualFirst.Text, "Second state of the group");
		Assert.AreEqual("initial", page.ManualSecond.Text, "Setter of the previous state should be rolled back");
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26248")]
	public async Task When_VisualState_Setter_Bound_And_GoToState_In_Group_With_StateTrigger()
	{
		var page = await LoadSetterBindingPage(new VisualStateSetterBindingViewModel());

		Assert.IsTrue(VisualStateManager.GoToState(page, "MixedManual", false));
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("flagged", page.MixedLabel.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_VisualState_Setter_Source_Changes_While_State_Active()
	{
		VisualStateSetterBindingViewModel viewModel = new();
		var page = await LoadSetterBindingPage(viewModel);
		Assert.IsTrue(VisualStateManager.GoToState(page, "ManualA", false));
		await TestServices.WindowHelper.WaitForIdle();

		viewModel.FlaggedText = "changed";
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("flagged", page.ManualFirst.Text, "The active setter should not follow the source");
		Assert.AreEqual("flagged", page.TriggeredFirst.Text, "The active setter should not follow the source");

		Assert.IsTrue(VisualStateManager.GoToState(page, "ManualB", false));
		Assert.IsTrue(VisualStateManager.GoToState(page, "ManualA", false));
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("changed", page.ManualFirst.Text, "Entering the state again should evaluate the binding");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ColumnDefinitions_Bound_And_Added_After_Load()
	{
		Grid grid = new() { DataContext = new VisualStateSetterBindingViewModel() };
		await UITestHelper.Load(grid, x => x.IsLoaded);

		ColumnDefinition[] columns = [new(), new()];
		foreach (var column in columns)
		{
			BindingOperations.SetBinding(column, ColumnDefinition.WidthProperty, new Binding { Path = new PropertyPath(nameof(VisualStateSetterBindingViewModel.ColWidth)) });
			grid.ColumnDefinitions.Add(column);
		}

		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(123d, columns[0].Width.Value);
		Assert.AreEqual(123d, columns[1].Width.Value);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26248")]
	public async Task When_Brush_Transform_Bound_And_Set_After_Load()
	{
		SolidColorBrush brush = new();
		Border border = new() { Width = 10, Height = 10, Background = brush, DataContext = new VisualStateSetterBindingViewModel() };
		await UITestHelper.Load(border, x => x.IsLoaded);

		TranslateTransform transform = new();
		BindingOperations.SetBinding(transform, TranslateTransform.XProperty, new Binding { Path = new PropertyPath(nameof(VisualStateSetterBindingViewModel.Offset)) });
		brush.Transform = transform;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(7d, transform.X);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_VisualState_Setter_DataContext_Arrives_While_State_Active()
	{
		var page = await LoadSetterBindingPage(viewModel: null);
		Assert.AreEqual("", page.AlwaysActiveLabel.Text, "The state applies a binding with no source as null");

		page.DataContext = new VisualStateSetterBindingViewModel();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("", page.AlwaysActiveLabel.Text, "The active setter should not follow the DataContext");
		Assert.AreEqual("flagged", page.TriggeredFirst.Text, "The trigger enters its state once the DataContext is known");

		page.DataContext = new VisualStateSetterBindingViewModel { FlaggedText = "second" };
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("flagged", page.TriggeredFirst.Text, "The active setter should not follow the DataContext");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Style_Setter_Bound_Does_Not_Resolve()
	{
		var textBlock = (TextBlock)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			$"<TextBlock {Ns} Text='initial'><TextBlock.Style><Style TargetType='TextBlock'><Setter Property='Tag' Value='plain' /><Setter Property='Text' Value='{{Binding FlaggedText}}' /></Style></TextBlock.Style></TextBlock>");
		textBlock.DataContext = new VisualStateSetterBindingViewModel();
		await UITestHelper.Load(textBlock, x => x.IsLoaded);

		Assert.AreEqual("plain", textBlock.Tag);
		Assert.AreEqual("initial", textBlock.Text);
	}

	private static async Task<When_VisualState_Setter_Binding> LoadSetterBindingPage(VisualStateSetterBindingViewModel? viewModel)
	{
		When_VisualState_Setter_Binding page = new();
		if (viewModel is not null)
		{
			page.DataContext = viewModel;
		}

		await UITestHelper.Load(page, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitForIdle();

		return page;
	}

	private static TextBlock? FindTextBlock(DependencyObject root, string name)
	{
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is TextBlock textBlock && textBlock.Name == name)
			{
				return textBlock;
			}

			if (FindTextBlock(child, name) is { } match)
			{
				return match;
			}
		}

		return null;
	}

	public sealed class Vm
	{
		public GridLength ColWidth { get; } = new GridLength(123);
		public GridLength RowHeight { get; } = new GridLength(45);
		public string Msg { get; } = "hello";
		public bool Flag { get; init; } = true;
	}
}
