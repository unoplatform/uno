#if __WASM__
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Uno_Foundation
{
	[TestClass]
	public class Given_WebAssemblyRuntime
	{
		[TestMethod]
		public async Task WhenPromiseReturnSynchronously()
		{
			var js = @"
				(function(){
				  return new Promise((ok, err)=> ok(""success""));
				})();";

			var result = await WebAssemblyRuntime.InvokeAsync(js);

			result.Should().Be("success");
		}

		[TestMethod]
		public async Task WhenPromiseReturnAsynchronously()
		{
			var js = @"
				(function(){
				  return new Promise((ok, err)=> setTimeout(()=>ok(""success"")));
				})();";

			var result = await WebAssemblyRuntime.InvokeAsync(js);

			result.Should().Be("success");
		}

		[TestMethod]
		public async Task WhenPromiseReturnErrorSynchronously()
		{
			var js = @"
				(function(){
				  return new Promise((ok, err)=> err(""error""));
				})();";

			Func<Task> Do = () => WebAssemblyRuntime.InvokeAsync(js);

			await Do.Should().ThrowAsync<Exception>().WithMessage("error");
		}

		[TestMethod]
		public async Task WhenPromiseReturnErrorAsynchronously()
		{
			var js = @"
				(function(){
				  return new Promise((ok, err)=> setTimeout(()=>err(""error"")));
				})();";

			Func<Task> Do = () => WebAssemblyRuntime.InvokeAsync(js);

			await Do.Should().ThrowAsync<Exception>().WithMessage("error");
		}

		[TestMethod]
		public async Task WhenPromiseThrowsExceptionDuringSetup()
		{
			var js = @"
				(function(){
				  throw ""error"";
				})();";

			Func<Task> Do = () => WebAssemblyRuntime.InvokeAsync(js);

			await Do.Should().ThrowAsync<Exception>().WithMessage("error");
		}
	}
}
#endif
