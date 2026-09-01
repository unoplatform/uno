using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Canaries for the Enter/Leave tree walk. The walk has two distinct recursion axes —
/// DP values (EnterProperties) and visual children (ChildEnter) — and they must stay
/// distinct: an element that is both a DP value and a visual child (ContentControl.Content,
/// Border.Child) would otherwise be entered twice. These tests pin the visit counts so a
/// refactor of the walk cannot silently change them.
/// </summary>
/// <remarks>
/// Joining a live tree is two passes, as in WinUI (CDOCollection::ChildEnter): a dead pass that
/// registers names, then the live one that does the work. The counts are tracked separately.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class Given_EnterLeaveWalk
{
	[TestMethod]
	public async Task When_Element_Enters_Tree_EnterImpl_Runs_Exactly_Once()
	{
		var leaf = new CountingBorder();
		var middle = new CountingBorder { Child = leaf };
		var root = new CountingBorder { Child = middle };

		await UITestHelper.Load(root, x => x.IsLoaded);

		Assert.AreEqual(1, root.LiveEnterCount, "root entered more than once");
		Assert.AreEqual(1, middle.LiveEnterCount, "middle entered more than once");
		Assert.AreEqual(1, leaf.LiveEnterCount, "leaf entered more than once");

		Assert.AreEqual(1, root.DeadEnterCount, "root registered its names more than once");
		Assert.AreEqual(1, middle.DeadEnterCount, "middle registered its names more than once");
		Assert.AreEqual(1, leaf.DeadEnterCount, "leaf registered its names more than once");

		TestServices.WindowHelper.WindowContent = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, root.LeaveCount, "root left more than once");
		Assert.AreEqual(1, middle.LeaveCount, "middle left more than once");
		Assert.AreEqual(1, leaf.LeaveCount, "leaf left more than once");
	}

	[TestMethod]
	public async Task When_Element_Enters_Tree_Depth_Is_Parent_Plus_One()
	{
		var leaf = new Border();
		var middle = new Border { Child = leaf };
		var root = new Border { Child = middle };

		await UITestHelper.Load(root, x => x.IsLoaded);

		Assert.AreEqual(root.Depth + 1, middle.Depth, "middle depth is not root + 1");
		Assert.AreEqual(middle.Depth + 1, leaf.Depth, "leaf depth is not middle + 1");

		TestServices.WindowHelper.WindowContent = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(int.MinValue, leaf.Depth, "depth was not reset on leave");
	}

	/// <summary>
	/// The element is added AFTER the host is already live, which drives the incremental-attach
	/// path (UIElement.AddChild -> ChildEnter) rather than the initial tree walk.
	/// </summary>
	[TestMethod]
	public async Task When_Element_Added_At_Runtime_EnterImpl_Runs_Exactly_Once()
	{
		var host = new Grid();
		await UITestHelper.Load(host, x => x.IsLoaded);

		var added = new CountingBorder { Child = new CountingBorder() };
		host.Children.Add(added);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, added.LiveEnterCount, "incrementally attached element entered more than once");
		Assert.AreEqual(1, ((CountingBorder)added.Child).LiveEnterCount, "its child entered more than once");

		Assert.AreEqual(1, added.DeadEnterCount, "incrementally attached element registered its names more than once");
	}

	/// <summary>
	/// The flyout's content is reached only through the FlyoutBase Enter chain (a dead enter that
	/// registers keyboard accelerators), not through the visual-children walk — the content is not
	/// in the visual tree. If a call site into that chain is ever rebound elsewhere, this drops to 0.
	/// </summary>
	[TestMethod]
	public async Task When_ContextFlyout_Owner_Enters_Tree_Content_Receives_Dead_Enter()
	{
		var flyoutContent = new CountingBorder();
		var owner = new Border
		{
			Width = 50,
			Height = 50,
			ContextFlyout = new Flyout { Content = flyoutContent },
		};

		await UITestHelper.Load(owner, x => x.IsLoaded);

		Assert.AreEqual(1, flyoutContent.DeadEnterCount, "context flyout content did not receive exactly one dead enter");
		Assert.AreEqual(0, flyoutContent.LiveEnterCount, "flyout content is not in the visual tree");
	}

	[TestMethod]
	public async Task When_Button_Flyout_Owner_Enters_Tree_Content_Receives_Dead_Enter()
	{
		var flyoutContent = new CountingBorder();
		var owner = new Button
		{
			Content = "x",
			Flyout = new Flyout { Content = flyoutContent },
		};

		await UITestHelper.Load(owner, x => x.IsLoaded);

		Assert.AreEqual(1, flyoutContent.DeadEnterCount, "button flyout content did not receive exactly one dead enter");
		Assert.AreEqual(0, flyoutContent.LiveEnterCount, "flyout content is not in the visual tree");
	}

	private partial class CountingBorder : Border
	{
		public int LiveEnterCount { get; private set; }

		public int DeadEnterCount { get; private set; }

		public int LeaveCount { get; private set; }

		internal override void EnterImpl(DependencyObject namescopeOwner, Uno.UI.Xaml.EnterParams @params)
		{
			if (@params.IsLive)
			{
				LiveEnterCount++;
			}
			else
			{
				DeadEnterCount++;
			}

			base.EnterImpl(namescopeOwner, @params);
		}

		internal override void LeaveImpl(DependencyObject namescopeOwner, Uno.UI.Xaml.LeaveParams @params)
		{
			LeaveCount++;
			base.LeaveImpl(namescopeOwner, @params);
		}
	}
}
