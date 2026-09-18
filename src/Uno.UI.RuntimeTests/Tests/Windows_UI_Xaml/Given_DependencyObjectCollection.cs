using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ObjectiveC;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_DependencyObjectCollection
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Add_Multiple_And_Invoke()
	{
		DependencyObjectCollection c = new();

		List<string> list = [];

		void One(object sender, object args) => list.Add("One");
		void Two(object sender, object args) => list.Add("Two");

		c.VectorChanged += One;
		c.VectorChanged += Two;
		c.VectorChanged += One;
		c.VectorChanged -= One;

		c.Add(c);

		Assert.IsTrue(list.SequenceEqual(["One", "Two"]));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Indexer_Get_IndexOutOfRange()
	{
		Assert.IsNull(new DependencyObjectCollection()[int.MaxValue]);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Indexer_Get_NegativeIndex()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new DependencyObjectCollection()[-1]);
	}

	/// <remarks>
	/// The theme-bound values live inside collections (GradientStops, Inlines) that the Leave and Enter
	/// walks visit by indexing the collection. A walk that skipped those items would leave the stale
	/// theme values in place when the subtree re-enters the tree under the other theme.
	/// </remarks>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Subtree_Reenters_Tree_Then_Items_In_Collections_Are_Updated()
	{
		try
		{
			var host = new Grid();
			var root = (Grid)XamlReader.Load(ThemedCollectionItemsXaml);
			root.RequestedTheme = ElementTheme.Light;
			host.Children.Add(root);

			await UITestHelper.Load(host);

			var stops = ((LinearGradientBrush)((Border)root.FindName("border")).Background).GradientStops;
			var run = (Run)((TextBlock)root.FindName("text")).Inlines[0];

			Assert.AreEqual(Microsoft.UI.Colors.Red, stops[0].Color);
			Assert.AreEqual(Microsoft.UI.Colors.Red, stops[1].Color);
			Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)run.Foreground).Color);

			host.Children.Remove(root);
			await TestServices.WindowHelper.WaitForIdle();

			root.RequestedTheme = ElementTheme.Dark;
			host.Children.Add(root);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(Microsoft.UI.Colors.Blue, stops[0].Color);
			Assert.AreEqual(Microsoft.UI.Colors.Blue, stops[1].Color);
			Assert.AreEqual(Microsoft.UI.Colors.Blue, ((SolidColorBrush)run.Foreground).Color);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	/// <remarks>
	/// Same collections as <see cref="When_Subtree_Reenters_Tree_Then_Items_In_Collections_Are_Updated"/>, but
	/// resolved by the Enter walk when the subtree joins the tree with a theme already set.
	/// </remarks>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Themed_Subtree_Enters_Tree_Then_Items_In_Collections_Are_Resolved()
	{
		try
		{
			var root = (Grid)XamlReader.Load(ThemedCollectionItemsXaml);
			root.RequestedTheme = ElementTheme.Dark;

			await UITestHelper.Load(root);

			var stops = ((LinearGradientBrush)((Border)root.FindName("border")).Background).GradientStops;
			var run = (Run)((TextBlock)root.FindName("text")).Inlines[0];

			Assert.AreEqual(Microsoft.UI.Colors.Blue, stops[0].Color);
			Assert.AreEqual(Microsoft.UI.Colors.Blue, stops[1].Color);
			Assert.AreEqual(Microsoft.UI.Colors.Blue, ((SolidColorBrush)run.Foreground).Color);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private const string ThemedCollectionItemsXaml = @"
		<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
			xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
			<Grid.Resources>
				<ResourceDictionary>
					<ResourceDictionary.ThemeDictionaries>
						<ResourceDictionary x:Key=""Light"">
							<Color x:Key=""TestCollectionItemColor"">#FFFF0000</Color>
							<SolidColorBrush x:Key=""TestCollectionItemBrush"" Color=""#FFFF0000"" />
						</ResourceDictionary>
						<ResourceDictionary x:Key=""Dark"">
							<Color x:Key=""TestCollectionItemColor"">#FF0000FF</Color>
							<SolidColorBrush x:Key=""TestCollectionItemBrush"" Color=""#FF0000FF"" />
						</ResourceDictionary>
					</ResourceDictionary.ThemeDictionaries>
				</ResourceDictionary>
			</Grid.Resources>
			<StackPanel>
				<Border x:Name=""border"" Width=""20"" Height=""20"">
					<Border.Background>
						<LinearGradientBrush>
							<GradientStop Offset=""0"" Color=""{ThemeResource TestCollectionItemColor}"" />
							<GradientStop Offset=""1"" Color=""{ThemeResource TestCollectionItemColor}"" />
						</LinearGradientBrush>
					</Border.Background>
				</Border>
				<TextBlock x:Name=""text""><Run Text=""Hello"" Foreground=""{ThemeResource TestCollectionItemBrush}"" /></TextBlock>
			</StackPanel>
		</Grid>";
}
