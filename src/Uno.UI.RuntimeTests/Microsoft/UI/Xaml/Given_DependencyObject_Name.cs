using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

// FrameworkElement.NameProperty is WinUI's DependencyObject_Name: x:Name storage for any DependencyObject.
[TestClass]
[RunsOnUIThread]
public class Given_DependencyObject_Name
{
	private const string Namespaces =
		"xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'";

	[TestMethod]
	public void When_NameProperty_Read_Twice_Then_Same_Instance()
	{
		Assert.AreSame(FrameworkElement.NameProperty, FrameworkElement.NameProperty);
	}

	[TestMethod]
	public void When_Brush_Default_Then_Empty_And_Unset()
	{
		var brush = new SolidColorBrush();

		Assert.AreEqual("", brush.GetValue(FrameworkElement.NameProperty));
		Assert.AreEqual(DependencyProperty.UnsetValue, brush.ReadLocalValue(FrameworkElement.NameProperty));
	}

	[TestMethod]
	public void When_Brush_NameProperty_Set_From_Code_Then_Stored_And_Cleared()
	{
		var brush = new SolidColorBrush();

		brush.SetValue(FrameworkElement.NameProperty, "X");

		Assert.AreEqual("X", brush.GetValue(FrameworkElement.NameProperty));
		Assert.AreEqual("X", brush.ReadLocalValue(FrameworkElement.NameProperty));

		brush.ClearValue(FrameworkElement.NameProperty);

		Assert.AreEqual("", brush.GetValue(FrameworkElement.NameProperty));
		Assert.AreEqual(DependencyProperty.UnsetValue, brush.ReadLocalValue(FrameworkElement.NameProperty));
	}

	[TestMethod]
	public void When_Brush_NameProperty_Changes_Then_Callback_Fires()
	{
		var brush = new SolidColorBrush();
		List<string> values = new();
		brush.RegisterPropertyChangedCallback(FrameworkElement.NameProperty, (s, dp) => values.Add((string)s.GetValue(dp)));

		brush.SetValue(FrameworkElement.NameProperty, "A");
		brush.SetValue(FrameworkElement.NameProperty, "B");
		brush.ClearValue(FrameworkElement.NameProperty);

		CollectionAssert.AreEqual(new[] { "A", "B", "" }, values);
	}

	[TestMethod]
	public void When_NameProperty_Set_To_Same_Value_Then_Callback_Still_Fires()
	{
		var brush = new SolidColorBrush();
		var border = new Border();
		var brushCount = 0;
		var borderCount = 0;
		brush.RegisterPropertyChangedCallback(FrameworkElement.NameProperty, (s, dp) => brushCount++);
		border.RegisterPropertyChangedCallback(FrameworkElement.NameProperty, (s, dp) => borderCount++);

		brush.ClearValue(FrameworkElement.NameProperty);
		brush.SetValue(FrameworkElement.NameProperty, "A");
		brush.SetValue(FrameworkElement.NameProperty, "A");
		border.Name = "A";
		border.Name = "A";
		border.Name = "A";

		Assert.AreEqual(3, brushCount);
		Assert.AreEqual(3, borderCount);
	}

	[TestMethod]
	public void When_FrameworkElement_NameProperty_Set_Then_Name_Mirrors()
	{
		var border = new Border();

		border.SetValue(FrameworkElement.NameProperty, "Q");
		Assert.AreEqual("Q", border.Name);

		border.Name = "R";
		Assert.AreEqual("R", border.GetValue(FrameworkElement.NameProperty));
	}

	[TestMethod]
	public void When_FrameworkElement_Name_Read_Then_NameProperty_Changes_Then_Name_Updates()
	{
		var border = new Border();
		Assert.AreEqual("", border.Name);

		border.SetValue(FrameworkElement.NameProperty, "Q");
		Assert.AreEqual("Q", border.Name);

		border.ClearValue(FrameworkElement.NameProperty);
		Assert.AreEqual("", border.Name);
	}

	[TestMethod]
	public void When_XName_On_Resource_Brush_Then_NameProperty_Holds_It()
	{
		var grid = (Grid)XamlReader.Load($"<Grid {Namespaces}><Grid.Resources><SolidColorBrush x:Key='k' x:Name='LB' Color='Red'/></Grid.Resources></Grid>");

		Assert.AreEqual("LB", ((DependencyObject)grid.Resources["k"]).GetValue(FrameworkElement.NameProperty));
	}

	[TestMethod]
	public void When_XName_On_MenuFlyout_Then_NameProperty_Holds_It()
	{
		var button = (Button)XamlReader.Load($"<Button {Namespaces}><Button.Flyout><MenuFlyout x:Name='LM'/></Button.Flyout></Button>");

		Assert.AreEqual("LM", button.Flyout.GetValue(FrameworkElement.NameProperty));
	}

	[TestMethod]
	public void When_XName_On_Run_Then_NameProperty_And_Name_Hold_It()
	{
		var textBlock = (TextBlock)XamlReader.Load($"<TextBlock {Namespaces}><Run x:Name='LR' Text='a'/></TextBlock>");
		var run = textBlock.Inlines[0];

		Assert.AreEqual("LR", run.GetValue(FrameworkElement.NameProperty));
		Assert.AreEqual("LR", run.Name);
	}

	[TestMethod]
	public void When_XName_On_VisualState_Then_NameProperty_And_Name_Hold_It()
	{
		var grid = (Grid)XamlReader.Load($"<Grid {Namespaces}><VisualStateManager.VisualStateGroups><VisualStateGroup x:Name='G'><VisualState x:Name='S'/></VisualStateGroup></VisualStateManager.VisualStateGroups></Grid>");
		var group = VisualStateManager.GetVisualStateGroups(grid)[0];
		var state = group.States[0];

		Assert.AreEqual("G", group.GetValue(FrameworkElement.NameProperty));
		Assert.AreEqual("G", group.Name);
		Assert.AreEqual("S", state.GetValue(FrameworkElement.NameProperty));
		Assert.AreEqual("S", state.Name);
	}

	[TestMethod]
	public void When_XName_On_Custom_DependencyObject_Then_NameProperty_Holds_It()
	{
		var grid = (Grid)XamlReader.Load($"<Grid {Namespaces} xmlns:local='using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml'><Grid.Resources><local:Given_DependencyObject_Name_PlainDO x:Key='k' x:Name='PD'/></Grid.Resources></Grid>");

		Assert.AreEqual("PD", ((DependencyObject)grid.Resources["k"]).GetValue(FrameworkElement.NameProperty));
	}

	[TestMethod]
	public void When_XName_On_Custom_DependencyObject_With_Name_Then_Only_NameProperty_Holds_It()
	{
		var grid = (Grid)XamlReader.Load($"<Grid {Namespaces} xmlns:local='using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml'><Grid.Resources><local:Given_DependencyObject_Name_NamedDO x:Key='k' x:Name='ND'/></Grid.Resources></Grid>");
		var namedDO = (Given_DependencyObject_Name_NamedDO)grid.Resources["k"];

		Assert.AreEqual("ND", namedDO.GetValue(FrameworkElement.NameProperty));
		Assert.IsNull(namedDO.Name);
	}

	[TestMethod]
	public void When_NameProperty_Set_On_Run_Then_Name_Mirrors()
	{
		var run = new Run();

		run.SetValue(FrameworkElement.NameProperty, "Q");

		Assert.AreEqual("Q", run.Name);
	}

	[TestMethod]
	public void When_NameProperty_Set_On_VisualState_Then_Name_Mirrors()
	{
		var group = new VisualStateGroup();
		var state = new VisualState();

		group.SetValue(FrameworkElement.NameProperty, "G");
		state.SetValue(FrameworkElement.NameProperty, "S");

		Assert.AreEqual("G", group.Name);
		Assert.AreEqual("S", state.Name);
	}

	[TestMethod]
	public void When_NameProperty_Set_To_Null_Then_Empty()
	{
		var brush = new SolidColorBrush();
		var border = new Border();

		brush.SetValue(FrameworkElement.NameProperty, null);
		border.Name = null;

		Assert.AreEqual("", brush.GetValue(FrameworkElement.NameProperty));
		Assert.AreEqual("", border.Name);
	}

	[TestMethod]
	public void When_Binding_Path_Name_To_Run_Then_Name()
	{
		var textBlock = (TextBlock)XamlReader.Load($"<TextBlock {Namespaces}><Run x:Name='BR' Text='a'/></TextBlock>");
		var target = new TextBlock();

		target.SetBinding(TextBlock.TextProperty, new Binding { Source = textBlock.Inlines[0], Path = new PropertyPath("Name") });

		Assert.AreEqual("BR", target.Text);
	}

	[TestMethod]
	public void When_Binding_Path_Name_To_Brush_Then_Empty()
	{
		var grid = (Grid)XamlReader.Load($"<Grid {Namespaces}><Grid.Resources><SolidColorBrush x:Key='k' x:Name='BB' Color='Red'/></Grid.Resources></Grid>");
		var target = new TextBlock();

		target.SetBinding(TextBlock.TextProperty, new Binding { Source = grid.Resources["k"], Path = new PropertyPath("Name") });

		Assert.AreEqual("", target.Text);
	}

#if __UNO__
	[TestMethod]
	public void When_Name_Looked_Up_By_Name_Then_Same_Instance()
	{
		Assert.AreSame(FrameworkElement.NameProperty, DependencyProperty.GetProperty(typeof(FrameworkElement), "Name"));
		Assert.AreSame(FrameworkElement.NameProperty, DependencyProperty.GetProperty(typeof(Border), "Name"));
		Assert.IsNull(DependencyProperty.GetProperty(typeof(SolidColorBrush), "Name"));
	}
#endif
}

public partial class Given_DependencyObject_Name_PlainDO : DependencyObject
{
}

public partial class Given_DependencyObject_Name_NamedDO : DependencyObject
{
	public string Name { get; set; }
}
