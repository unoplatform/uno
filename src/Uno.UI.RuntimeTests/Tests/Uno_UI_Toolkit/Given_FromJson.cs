#if !WINAPPSDK
#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Helpers;
using Uno.Xaml;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Toolkit;

[TestClass]
[RunsOnUIThread]
public partial class Given_FromJson
{
	[TestMethod]
	public async Task When_DataContext_Comes_From_Json()
	{
		var grid = XamlHelper.LoadXaml<Grid>(
"""
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:markup="using:Uno.UI.Markup"
      xmlns:xml="http://www.w3.org/XML/1998/namespace">
	<Grid.Resources>
		<x:String x:Key="SampleData" xml:space="preserve">
{
    "Title": "Runtime Test",
    "Owner": {
        "Name": "Uno Platform"
    },
    "Priority": 3,
    "Numbers": [ 1, 2, 3 ]
}
		</x:String>
	</Grid.Resources>

	<TextBlock x:Name="Target"
	           DataContext="{markup:FromJson Source={StaticResource SampleData}}"
	           Text="{Binding Owner.Name}" />
</Grid>
""",
			autoInjectXmlns: false);

		WindowHelper.WindowContent = grid;
		var target = (TextBlock)grid.FindName("Target");
		await WindowHelper.WaitForLoaded(target);

		Assert.AreEqual("Uno Platform", target.Text);

		if (target.DataContext is not IDictionary<string, object?> dictionary)
		{
			Assert.Fail("FromJson did not produce an ExpandoObject-backed dictionary.");
			return;
		}

		Assert.AreEqual("Runtime Test", dictionary["Title"]);
		Assert.AreEqual(3, dictionary["Priority"]);

		if (dictionary["Owner"] is not IDictionary<string, object?> owner)
		{
			Assert.Fail("Owner property was not converted into a dictionary.");
			return;
		}

		Assert.AreEqual("Uno Platform", owner["Name"]);

		if (dictionary["Numbers"] is not IList<object?> numbers)
		{
			Assert.Fail("Numbers property was not converted into a list.");
			return;
		}

		Assert.HasCount(3, numbers);
		Assert.AreEqual(1, numbers[0]);
		Assert.AreEqual(2, numbers[1]);
		Assert.AreEqual(3, numbers[2]);
	}

	[TestMethod]
	public async Task When_DataContext_Feeds_ItemsControl()
	{
		var grid = XamlHelper.LoadXaml<Grid>(
"""
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:markup="using:Uno.UI.Markup"
      xmlns:xml="http://www.w3.org/XML/1998/namespace">
	<Grid.Resources>
		<x:String x:Key="ProjectData" xml:space="preserve">
{
    "Project": "Feature Work",
    "Tasks": [
        { "Title": "Design" },
        { "Title": "Implementation" }
    ]
}
		</x:String>
	</Grid.Resources>

	<StackPanel x:Name="ProjectPanel"
	            DataContext="{markup:FromJson Source={StaticResource ProjectData}}"
	            Spacing="8">
		<TextBlock x:Name="ProjectTitle"
		           Text="{Binding Project}" />
		<ListView x:Name="TasksList"
		         ItemsSource="{Binding Tasks}">
			<ListView.ItemTemplate>
				<DataTemplate>
					<TextBlock Text="{Binding Title}" />
				</DataTemplate>
			</ListView.ItemTemplate>
		</ListView>
	</StackPanel>
</Grid>
""",
			autoInjectXmlns: false);

		WindowHelper.WindowContent = grid;
		var listView = (ListView)grid.FindName("TasksList");
		var title = (TextBlock)grid.FindName("ProjectTitle");
		await WindowHelper.WaitForLoaded(listView);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("Feature Work", title.Text);
		Assert.HasCount(2, listView.Items, "Tasks list should expose two entries from the JSON payload.");

		if (listView.Items[0] is not IDictionary<string, object?> firstTask ||
			listView.Items[1] is not IDictionary<string, object?> secondTask)
		{
			Assert.Fail("ListView items were not converted into dictionaries.");
			return;
		}

		Assert.AreEqual("Design", firstTask["Title"]);
		Assert.AreEqual("Implementation", secondTask["Title"]);
	}

	[TestMethod]
	public void When_Invalid_Json_Throws_Parse_Exception()
	{
		const string xaml =
"""
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:markup="using:Uno.UI.Markup"
      xmlns:xml="http://www.w3.org/XML/1998/namespace">
	<Grid.Resources>
		<x:String x:Key="InvalidJson" xml:space="preserve">
{
    "Title": "Missing Terminator"
	"Value": 42

		</x:String>
	</Grid.Resources>

	<TextBlock DataContext="{markup:FromJson Source={StaticResource InvalidJson}}" />
</Grid>
""";

		Assert.ThrowsExactly<XamlParseException>(() => XamlHelper.LoadXaml<Grid>(xaml, autoInjectXmlns: false));
	}

	[TestMethod]
	public void When_Empty_Json_Throws_Parse_Exception()
	{
		const string xaml =
"""
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:markup="using:Uno.UI.Markup"
      xmlns:xml="http://www.w3.org/XML/1998/namespace">
	<Grid.Resources>
		<x:String x:Key="EmptyJson" xml:space="preserve">

		</x:String>
	</Grid.Resources>

	<TextBlock DataContext="{markup:FromJson Source={StaticResource EmptyJson}}" />
</Grid>
""";

		Assert.ThrowsExactly<XamlParseException>(() => XamlHelper.LoadXaml<Grid>(xaml, autoInjectXmlns: false));
	}

	[TestMethod]
	public void When_Number_Exceeds_Int32_Uses_Double()
	{
		const string xaml =
"""
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:markup="using:Uno.UI.Markup"
      xmlns:xml="http://www.w3.org/XML/1998/namespace">
	<Grid.Resources>
		<x:String x:Key="LargeNumber" xml:space="preserve">
{
    "Value": 5000000000
}
		</x:String>
	</Grid.Resources>

	<TextBlock x:Name="Target"
			   DataContext="{markup:FromJson Source={StaticResource LargeNumber}}"
	           Text="{Binding Value}" />
</Grid>
""";

		var grid = XamlHelper.LoadXaml<Grid>(xaml, autoInjectXmlns: false);
		WindowHelper.WindowContent = grid;
		var textBlock = (TextBlock)grid.FindName("Target");
		Assert.IsInstanceOfType(textBlock.DataContext, typeof(IDictionary<string, object?>));
		var data = (IDictionary<string, object?>)textBlock.DataContext!;
		Assert.IsInstanceOfType(data["Value"], typeof(double));
		Assert.AreEqual(5000000000d, (double)data["Value"]!);
	}

	[TestMethod]
	public void When_Number_Within_Int32_Limits_Stays_Int()
	{
		const string xaml =
"""
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:markup="using:Uno.UI.Markup"
      xmlns:xml="http://www.w3.org/XML/1998/namespace">
	<Grid.Resources>
		<x:String x:Key="IntNumber" xml:space="preserve">
{
    "Value": 123456789
}
		</x:String>
	</Grid.Resources>

	<TextBlock x:Name="Target"
			    DataContext="{markup:FromJson Source={StaticResource IntNumber}}"
	           Text="{Binding Value}" />
</Grid>
""";

		var grid = XamlHelper.LoadXaml<Grid>(xaml, autoInjectXmlns: false);
		WindowHelper.WindowContent = grid;
		var textBlock = (TextBlock)grid.FindName("Target");
		var data = (IDictionary<string, object?>)textBlock.DataContext!;
		Assert.IsInstanceOfType(data["Value"], typeof(int));
		Assert.AreEqual(123456789, (int)data["Value"]!);
	}

	[TestMethod]
	public async Task When_DataContext_Uses_Source_Attribute()
	{
		var page = XamlHelper.LoadXaml<Page>(
"""
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:markup="using:Uno.UI.Markup"
      xmlns:xml="http://www.w3.org/XML/1998/namespace">
	<Page.Resources>
		<x:String x:Key="InlineJson" xml:space="preserve">
{
    "Value": "Inline"
}
		</x:String>
	</Page.Resources>

	<Page.DataContext>
		<markup:FromJson Source="{StaticResource InlineJson}" />
	</Page.DataContext>

	<TextBlock x:Name="InlineTarget" Text="{Binding Value}" />
</Page>
""",
			autoInjectXmlns: false);

		WindowHelper.WindowContent = page;
		var textBlock = (TextBlock)page.FindName("InlineTarget");
		await WindowHelper.WaitForLoaded(textBlock);
		Assert.AreEqual("Inline", textBlock.Text);
	}

	[TestMethod]
	public async Task When_DataContext_Uses_Content_Property()
	{
		var page = XamlHelper.LoadXaml<Page>(
"""
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:markup="using:Uno.UI.Markup"
      xmlns:xml="http://www.w3.org/XML/1998/namespace">
	<Page.DataContext>
		<markup:FromJson>
			<markup:FromJson.Source>
                {
                    "Value": "Content"
                }
			</markup:FromJson.Source>
		</markup:FromJson>
	</Page.DataContext>

	<TextBlock x:Name="ContentTarget" Text="{Binding Value}" />
</Page>
""",
			autoInjectXmlns: false);

		WindowHelper.WindowContent = page;
		var textBlock = (TextBlock)page.FindName("ContentTarget");
		await WindowHelper.WaitForLoaded(textBlock);
		Assert.AreEqual("Content", textBlock.Text);
	}
}
#endif
