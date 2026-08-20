#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_ResourceResolver_Scopes
{
	[TestMethod]
	public async Task When_Scopes_Are_Used_On_Parallel_Threads()
	{
		using var firstPushed = new ManualResetEventSlim();
		using var secondPushed = new ManualResetEventSlim();
		using var firstChecked = new ManualResetEventSlim();
		var firstScope = XamlScope.Create();
		var secondScope = XamlScope.Create();

		var first = Task.Run(() =>
		{
			ResourceResolver.PushNewScope(firstScope);
			try
			{
				firstPushed.Set();
				secondPushed.Wait();
				Assert.AreEqual(firstScope, ResourceResolver.CurrentScope);
			}
			finally
			{
				ResourceResolver.PopScope();
				firstChecked.Set();
			}
		});

		var second = Task.Run(() =>
		{
			firstPushed.Wait();
			ResourceResolver.PushNewScope(secondScope);
			try
			{
				secondPushed.Set();
				firstChecked.Wait();
				Assert.AreEqual(secondScope, ResourceResolver.CurrentScope);
			}
			finally
			{
				ResourceResolver.PopScope();
			}
		});

		await Task.WhenAll(first, second);
	}
}
