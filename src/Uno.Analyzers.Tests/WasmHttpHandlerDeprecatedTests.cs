using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using Uno.Analyzers.Tests.Verifiers;
using System.Threading.Tasks;

namespace Uno.Analyzers.Tests
{
	using Verify = CSharpCodeFixVerifier<WasmHttpHandlerDeprecatedAnalyzer, EmptyCodeFixProvider>;

	[TestClass]
	public class WasmHttpHandlerDeprecatedTests
	{
		private static string Stub = @"
namespace Uno.UI.Wasm
{
	public class WasmHttpHandler
	{
		public void M() { }
	}
}
";

		[TestMethod]
		public async Task When_Method_Is_Invoked()
		{
			var test = @"
using Uno.UI.Wasm;

public class C
{
	public void M(WasmHttpHandler handler) => [|handler.M()|];
}
" + Stub;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_Object_Is_Instantiated()
		{
			var test = @"
using Uno.UI.Wasm;

public class C
{
	public void M1() => _ = [|new WasmHttpHandler()|];
	public void M2() { WasmHttpHandler handler = [|new()|]; }
}
" + Stub;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_Inherited()
		{
			var test = @"
using Uno.UI.Wasm;

public class [|C|] : WasmHttpHandler
{
}
" + Stub;

			await Verify.VerifyAnalyzerAsync(test);
		}
	}
}
