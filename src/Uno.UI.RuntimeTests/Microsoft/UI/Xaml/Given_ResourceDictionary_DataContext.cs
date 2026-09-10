#if HAS_UNO
#nullable enable

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ResourceDictionary_DataContext
	{
		// Resources stored in a ResourceDictionary must not inherit/cache the DataContext of the subtree
		// they are applied into (WinUI parity — resources have no DataContext). Caching it would keep that
		// DataContext, and for a previewed app its collectible AssemblyLoadContext, alive forever via the
		// long-lived dictionary. These runtime tests guard the reviewer concern that this block does not
		// regress style-based binding DataContext propagation: a Style is itself a resource, but the element
		// it is applied to is not, so the styled element must keep inheriting DataContext and its bindings
		// must still resolve.

		[TestMethod]
		public async Task When_Resource_Style_Applied_Then_Styled_Element_Still_Resolves_DataContext_Binding()
		{
			var root = (Grid)XamlReader.Load(
				"""
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					  Width="100"
					  Height="100">
					<Grid.Resources>
						<Style x:Key="ProbeStyle" TargetType="Border">
							<Setter Property="MinWidth" Value="10" />
						</Style>
					</Grid.Resources>
					<Border Style="{StaticResource ProbeStyle}" Tag="{Binding Value}" />
				</Grid>
				""");

			var probe = (Border)root.Children[0];
			root.DataContext = new BindingProbe { Value = "bound!" };

			await UITestHelper.Load(root, x => x.IsLoaded);

			Assert.AreEqual(
				"bound!",
				probe.Tag,
				"applying a (resource) Style must not strip the styled element's inherited DataContext; " +
				"its {Binding} must still resolve.");
		}

		[TestMethod]
		public async Task When_Resource_In_DataContext_Subtree_Then_Resource_Has_No_DataContext_But_Child_Does()
		{
			var root = (Grid)XamlReader.Load(
				"""
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					  Width="100"
					  Height="100">
					<Grid.Resources>
						<Border x:Key="ResBorder" />
					</Grid.Resources>
					<Border />
				</Grid>
				""");

			var resourceBorder = (Border)root.Resources["ResBorder"];
			var child = (Border)root.Children[0];
			var dataContext = new BindingProbe { Value = "host" };
			root.DataContext = dataContext;

			await UITestHelper.Load(root, x => x.IsLoaded);

			Assert.IsNull(
				resourceBorder.DataContext,
				"a resource must not inherit the host subtree's DataContext (WinUI parity); inheriting it would " +
				"pin that DataContext — and a previewed app's collectible AssemblyLoadContext — alive via the dictionary.");

			Assert.AreSame(
				dataContext,
				child.DataContext,
				"an ordinary logical child must still inherit DataContext; only ResourceDictionary items are exempt.");
		}

		private sealed class BindingProbe
		{
			public string? Value { get; set; }
		}
	}
}
#endif
