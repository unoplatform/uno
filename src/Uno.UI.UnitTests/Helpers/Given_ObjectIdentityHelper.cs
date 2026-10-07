#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers.WinUI;

namespace Uno.UI.Tests.Helpers;

[TestClass]
public class Given_ObjectIdentityHelper
{
	[TestMethod]
	public void When_Same_Object_Then_Same_Id()
	{
		object o = new();

		Assert.AreEqual(ObjectIdentityHelper.GetId(o), ObjectIdentityHelper.GetId(o));
		Assert.AreNotEqual(0UL, ObjectIdentityHelper.GetId(o));
	}

	[TestMethod]
	public void When_Distinct_Objects_Then_Distinct_Ids()
		=> Assert.AreNotEqual(ObjectIdentityHelper.GetId(new object()), ObjectIdentityHelper.GetId(new object()));

	[TestMethod]
	public void When_Objects_Are_Equal_But_Not_Same_Then_Distinct_Ids()
	{
		var a = new string('x', 3);
		var b = new string('x', 3);

		Assert.AreEqual(a, b);
		Assert.AreNotEqual(ObjectIdentityHelper.GetId(a), ObjectIdentityHelper.GetId(b));
	}

	[TestMethod]
	public void When_Object_Collected_Then_Not_Kept_Alive()
	{
		var weak = Track();

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Assert.IsFalse(weak.IsAlive);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference Track()
	{
		object o = new();
		ObjectIdentityHelper.GetId(o);
		return new WeakReference(o);
	}
}
