#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_ResourceResolver_Scopes
{
	[TestMethod]
	public async Task When_Parallel_Threads_Push_Distinct_Scopes_Each_Thread_Sees_Its_Own()
	{
		using var firstPushed = new ManualResetEventSlim();
		using var secondPushed = new ManualResetEventSlim();
		using var firstChecked = new ManualResetEventSlim();
		var firstSource = new object();
		var secondSource = new object();
		var firstReference = WeakReferencePool.RentWeakReference(this, firstSource);
		var secondReference = WeakReferencePool.RentWeakReference(this, secondSource);

		try
		{
			var firstScope = XamlScope.Create().Push(firstReference);
			var secondScope = XamlScope.Create().Push(secondReference);
			Assert.AreNotEqual(firstScope, secondScope);

			var first = Task.Run(() =>
			{
				ResourceResolver.PushNewScope(firstScope);
				try
				{
					firstPushed.Set();
					secondPushed.Wait();

					// A shared process-wide stack exposes secondScope here, so this
					// assertion fails before ResourceResolver scopes are thread-local.
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
		finally
		{
			WeakReferencePool.ReturnWeakReference(this, firstReference);
			WeakReferencePool.ReturnWeakReference(this, secondReference);
		}
	}
}
