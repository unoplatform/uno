#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Name lookup on a live tree: FindName resolves from the namescope, and the runtime XAML reader
/// gives every root - including every template instantiation - a namescope of its own.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_NameScopeLookup
{
	[TestMethod]
	public async Task When_Loaded_Element_Is_Found_By_Name()
	{
		var named = new Border { Name = "lookup_target" };
		var host = new Grid();
		host.Children.Add(named);
		await UITestHelper.Load(host, x => x.IsLoaded);

		Assert.AreEqual(named, host.FindName("lookup_target"));
	}

	/// <summary>
	/// Each instantiation of a template gets its own scope, so the same x:Name resolves to the
	/// instance that owns it rather than to whichever was created last.
	/// </summary>
	[TestMethod]
	public async Task When_Template_Instantiated_Twice_Each_Name_Stays_In_Its_Own_Scope()
	{
		var template = (DataTemplate)XamlReader.Load(
			"""
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Border x:Name="templatedBorder" Width="10" Height="10" />
			</DataTemplate>
			""");

		var first = (Border)template.LoadContent()!;
		var second = (Border)template.LoadContent()!;
		var host = new StackPanel();
		host.Children.Add(first);
		host.Children.Add(second);
		await UITestHelper.Load(host, x => x.IsLoaded);

		Assert.AreNotSame(first, second);

		// Strict mode takes the tree walk out of the picture, so this can only pass through the
		// per-instantiation namescope the reader attaches.
		Uno.UI.FeatureConfiguration.FrameworkElement.UseLegacyFindNameTreeWalk = false;
		try
		{
			Assert.AreEqual(first, first.FindName("templatedBorder"));
			Assert.AreEqual(second, second.FindName("templatedBorder"));
		}
		finally
		{
			Uno.UI.FeatureConfiguration.FrameworkElement.UseLegacyFindNameTreeWalk = true;
		}
	}
}
