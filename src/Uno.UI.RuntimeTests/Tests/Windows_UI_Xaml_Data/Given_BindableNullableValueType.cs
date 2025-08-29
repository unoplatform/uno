using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

[TestClass]
public class Given_BindableNullableValueType
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_BindableNullableValueTypeTestPage()
	{
		var x = new BindableNullableValueTypeTestPage();
		var tb = x.textBlock;
		tb.Tag = "10";
#if WINAPPSDK
		Assert.AreEqual(null, x.MyProperty);
#else
		Assert.AreEqual(10, x.MyProperty);
#endif
		tb.Tag = null;
		Assert.IsNull(x.MyProperty);
	}
}
