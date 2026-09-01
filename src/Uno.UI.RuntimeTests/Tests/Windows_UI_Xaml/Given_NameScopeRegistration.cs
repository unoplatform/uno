#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Core.NameScoping;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Names follow the tree: the Enter walk registers an element's x:Name in the scope it joins, and
/// the Leave walk takes it back out.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_NameScopeRegistration
{
	private static DependencyObject? Lookup(DependencyObject scopeMember, string name)
		=> scopeMember.GetContext().GetNamedObject(name, scopeMember.GetStandardNameScopeOwner(), NameScopeType.StandardNameScope);

	[TestMethod]
	public async Task When_Named_Element_Enters_Tree_Its_Name_Registers()
	{
		var host = new Grid();
		await UITestHelper.Load(host, x => x.IsLoaded);

		var named = new Border { Name = "step9_entering" };
		host.Children.Add(named);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(named, Lookup(host, "step9_entering"));
	}

	[TestMethod]
	public async Task When_Named_Element_Leaves_Tree_Its_Name_Unregisters()
	{
		var host = new Grid();
		var named = new Border { Name = "step9_leaving" };
		host.Children.Add(named);
		await UITestHelper.Load(host, x => x.IsLoaded);
		Assert.AreEqual(named, Lookup(host, "step9_leaving"), "the name was never registered");

		host.Children.Remove(named);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(Lookup(host, "step9_leaving"));
	}

	/// <summary>
	/// WinUI clears the name before unregistering it, so a rename strands the old entry
	/// (depends.cpp:624-628). Uno deliberately unregisters the old name for real.
	/// </summary>
	[TestMethod]
	public async Task When_Named_Element_Is_Renamed_The_Table_Follows()
	{
		var host = new Grid();
		var named = new Border { Name = "step9_before" };
		host.Children.Add(named);
		await UITestHelper.Load(host, x => x.IsLoaded);

		named.Name = "step9_after";
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(named, Lookup(host, "step9_after"));
		Assert.IsNull(Lookup(host, "step9_before"), "the old name must not be stranded in the table");
	}

	/// <summary>
	/// Two elements claiming one name is last-one-wins; the loser leaving must not take the winner's
	/// entry with it.
	/// </summary>
	[TestMethod]
	public async Task When_Duplicate_Name_Loser_Leaves_The_Winner_Keeps_The_Name()
	{
		var host = new Grid();
		var first = new Border { Name = "step9_duplicate" };
		var second = new Border { Name = "step9_duplicate" };
		host.Children.Add(first);
		host.Children.Add(second);
		await UITestHelper.Load(host, x => x.IsLoaded);
		Assert.AreEqual(second, Lookup(host, "step9_duplicate"), "the last registration wins");

		host.Children.Remove(first);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(second, Lookup(host, "step9_duplicate"), "the loser's leave must not clear the winner's entry");
	}

	/// <summary>
	/// A name set while the element is detached has no scope to land in; it registers when the
	/// element joins one.
	/// </summary>
	[TestMethod]
	public async Task When_Name_Set_While_Detached_It_Registers_On_Enter()
	{
		var host = new Grid();
		await UITestHelper.Load(host, x => x.IsLoaded);

		var named = new Border();
		named.Name = "step9_detached";
		Assert.IsNull(Lookup(host, "step9_detached"));

		host.Children.Add(named);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(named, Lookup(host, "step9_detached"));
	}

}
