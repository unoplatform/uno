﻿#nullable enable
#if !WINAPPSDK
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_xUid
{
	[TestMethod]
	public void When_xUid()
	{
		var SUT = new When_xUid();

		TestServices.WindowHelper.WindowContent = SUT;

		Assert.AreEqual("en-US Value for When_xUid", SUT.defaultResolver.Text);
		Assert.AreEqual("en-US Value for When_xUid_Explicit in TopLevelNamedRuntimeTests", SUT.namedResolver.Text);
		Assert.AreEqual("en-US Value for SomePrefix/When_xUid_With_Prefix", SUT.defaultResolverWithPrefix.Text);
	}

	[TestMethod]
	public void When_xUid_On_Root()
	{
		var SUT = new When_xUid_On_Root();
		Assert.AreEqual("en-US Value for When_xUid_Root.Title", SUT.Title);
	}
}
#endif
