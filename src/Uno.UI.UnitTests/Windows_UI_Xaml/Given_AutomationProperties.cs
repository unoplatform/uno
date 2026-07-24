#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_AutomationProperties
{
	[TestMethod]
	public void When_Relation_Collection_Outlives_Owner_Owner_Is_Collectible()
	{
		var describedBy = new DependencyObjectCollection();
		var owner = CreateOwner(describedBy);

		for (var i = 0; i < 3 && owner.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		Assert.IsFalse(owner.IsAlive, "The relation collection subscription must not retain its owner.");
		GC.KeepAlive(describedBy);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateOwner(DependencyObjectCollection describedBy)
	{
		var owner = new Border();
		owner.SetValue(AutomationProperties.DescribedByProperty, describedBy);
		return new WeakReference(owner);
	}
}
