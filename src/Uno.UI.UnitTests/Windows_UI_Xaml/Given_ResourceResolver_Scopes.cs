#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[UITestClass]
public class Given_ResourceResolver_Scopes
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25231")]
	public void When_Base_Scope_Popped_Base_Scope_Is_Kept()
	{
		var baseScope = ResourceResolver.CurrentScope;

		Assert.ThrowsExactly<InvalidOperationException>(ResourceResolver.PopScope);

		Assert.AreEqual(baseScope, ResourceResolver.CurrentScope);
		ResourceResolver.PushNewScope(XamlScope.Create());
		ResourceResolver.PopScope();
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25231")]
	public void When_Source_Popped_From_Empty_Scope_Scope_Is_Kept()
	{
		ResourceResolver.PushNewScope(XamlScope.Create());
		try
		{
			Assert.ThrowsExactly<InvalidOperationException>(ResourceResolver.PopSourceFromScope);
		}
		finally
		{
			// Throws if the failed pop dropped the pushed scope, as only the base scope would remain.
			ResourceResolver.PopScope();
		}
	}
}
