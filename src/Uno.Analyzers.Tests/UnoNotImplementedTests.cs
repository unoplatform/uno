using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using Uno.Analyzers.Tests.Verifiers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Uno.Analyzers.Tests
{
	using Verify = CSharpCodeFixVerifier<UnoNotImplementedAnalyzer, EmptyCodeFixProvider>;

	[TestClass]
	public class UnoNotImplementedTests
	{
		private static string UnoNotImplementedAtribute = @"
		#nullable enable
		namespace Uno
		{
				[System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
				public sealed class NotImplementedAttribute : Attribute
				{
					public NotImplementedAttribute() { }

					public NotImplementedAttribute(params string[] platforms)
					{
						Platforms = platforms;
					}

					public string[]? Platforms { get; }
				}
		}";

		private static async Task TestWithPreprocessorDirective(string testCode, IEnumerable<string> preprocessorSymbols)
		{
			await new Verify.Test
			{
				TestCode = testCode,
				FixedCode = testCode,
				PreprocessorSymbols = preprocessorSymbols,
			}.RunAsync();
		}

		[TestMethod]
		public async Task Nothing()
		{
			var test = @"";

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_EmptyNotImplemented()
		{
			var test = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                using System.Diagnostics;

				namespace Uno
				{
					[NotImplemented]
					public class TestClass { }
				}

                namespace ConsoleApplication1
                {
                    class TypeName
                    {
                        public TypeName()
                        {
                           var a = [|new Uno.TestClass()|];
                        }
                    }
                }

			" + UnoNotImplementedAtribute;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_EventAddNotImplemented()
		{
			var test =
				"""
					using System;
					using System.Collections.Generic;
					using System.Linq;
					using System.Text;
					using System.Threading.Tasks;
					using System.Diagnostics;

					namespace Uno
					{
						public class TestClass 
						{ 
							[NotImplemented]
							public event Action MyEvent;
						}
					}

					namespace ConsoleApplication1
					{
					    class TypeName
					    {
					        public TypeName()
					        {
					           var a = new Uno.TestClass();
							   [|a.MyEvent|] += delegate { };
					        }
					    }
					}
				""" + UnoNotImplementedAtribute;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_EventRemoveNotImplemented()
		{
			var test =
				"""
					using System;
					using System.Collections.Generic;
					using System.Linq;
					using System.Text;
					using System.Threading.Tasks;
					using System.Diagnostics;

					namespace Uno
					{
						public class TestClass 
						{ 
							[NotImplemented]
							public event Action MyEvent;
						}
					}

					namespace ConsoleApplication1
					{
					    class TypeName
					    {
					        public TypeName()
					        {
					           var a = new Uno.TestClass();
							   [|a.MyEvent|] -= delegate { };
					        }
					    }
					}
				""" + UnoNotImplementedAtribute;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_SinglePlatform_Included()
		{
			var test = """
				using System;

				namespace Uno
				{
					[NotImplemented("__WASM__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = [|new Uno.TestClass()|];
						}
					}
				}
				""" + UnoNotImplementedAtribute;
			await TestWithPreprocessorDirective(test, new[] { "__WASM__" });
		}

		[TestMethod]
		public async Task When_SinglePlatform_Excluded()
		{
			var test = """
				using System;

				namespace Uno
				{
					[NotImplemented("__SKIA__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = new Uno.TestClass();
						}
					}
				}
				""" + UnoNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "__WASM__" });
		}

		[TestMethod]
		public async Task When_TwoPlatforms_Excluded()
		{
			var test = """
				using System;

				namespace Uno
				{
					[NotImplemented("__SKIA__", "__IOS__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = new Uno.TestClass();
						}
					}
				}
				""" + UnoNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "__WASM__" });
		}

		[TestMethod]
		public async Task When_Generic_Excluded()
		{
			var test = """
				using System;

				namespace Uno
				{
					[NotImplemented("__IOS__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = new Uno.TestClass();
						}
					}
				}
				""" + UnoNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "UNO_REFERENCE_API" });
		}

		[TestMethod]
		public async Task When_Generic_Partial_Excluded()
		{
			var test = """
				using System;

				namespace Uno
				{
					[NotImplemented("__SKIA__", "__IOS__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = new Uno.TestClass();
						}
					}
				}
				""" + UnoNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "UNO_REFERENCE_API" });
		}

		[TestMethod]
		public async Task When_Generic_Included()
		{
			var test = """
				using System;

				namespace Uno
				{
					[NotImplemented("__SKIA__", "__IOS__", "__WASM__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = [|new Uno.TestClass()|];
						}
					}
				}
				""" + UnoNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "UNO_REFERENCE_API" });
		}


		[TestMethod]
		public async Task When_Generic_Member_Included()
		{
			var test = """
				using System;

				namespace Uno
				{
					public class TestClass {
						[NotImplemented("__SKIA__", "__IOS__", "__WASM__")]
						public int Test { get; }
					}
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = [|new Uno.TestClass().Test|];
							var b = new Uno.TestClass()?[|.Test|];
						}
					}
				}
				""" + UnoNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "UNO_REFERENCE_API" });
		}

		[TestMethod]
		public async Task When_Generic_Member_Partial_Excluded()
		{
			var test = """
				using System;

				namespace Uno
				{
					public class TestClass {
						[NotImplemented("__IOS__", "__WASM__")]
						public int Test { get; }
					}
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = new Uno.TestClass().Test;
						}
					}
				}
				""" + UnoNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "UNO_REFERENCE_API" });
		}

		[TestMethod]
		public async Task When_Using_Object_Initializer_Syntax_Included()
		{
			var test = """
				using System;

				namespace Uno
				{
					public class TestClass {
						[NotImplemented("__SKIA__", "__IOS__", "__WASM__")]
						public int Test { get; set; }
					}
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var x = new Uno.TestClass { [|Test|] = 0 };
						}
					}
				}
				""" + UnoNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "UNO_REFERENCE_API" });
		}
	}
}
