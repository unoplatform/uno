using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

[TestClass]
[RunsOnUIThread]
public class Given_InlineCollection
{
	[TestMethod]
	public async Task When_Implicit_Items_On_Null_Default_Collection()
	{
		var control = new InlineCollection_NullDefault();

		try
		{
			await UITestHelper.Load(control, x => x.IsLoaded);

			Assert.HasCount(1, control.Transitions);
			Assert.IsInstanceOfType<EntranceThemeTransition>(control.Transitions[0]);

			var childrenTransitions = control.RootPanelElement.ChildrenTransitions;
			Assert.HasCount(2, childrenTransitions);
			Assert.IsInstanceOfType<RepositionThemeTransition>(childrenTransitions[0]);
			Assert.IsInstanceOfType<EntranceThemeTransition>(childrenTransitions[1]);

			Assert.HasCount(1, control.ContentHostElement.ContentTransitions);

			var host = control.HostElement;
			Assert.IsNotNull(host.NullDefaultItems);
			Assert.HasCount(2, host.NullDefaultItems);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Implicit_Items_On_Non_Null_Default_Collection()
	{
		var control = new InlineCollection_NullDefault();

		try
		{
			await UITestHelper.Load(control, x => x.IsLoaded);

			// Items go into the existing instance, which is not replaced.
			var host = control.HostElement;
			Assert.AreSame(host.InitialDefaultItems, host.DefaultItems);
			Assert.HasCount(1, host.DefaultItems);

			var group = (TransformGroup)control.TransformedBorderElement.RenderTransform;
			Assert.HasCount(2, group.Children);
			Assert.AreEqual(10, ((RotateTransform)group.Children[0]).Angle);
			Assert.AreEqual(2, ((ScaleTransform)group.Children[1]).ScaleX);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
