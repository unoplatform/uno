using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Uno.Analyzers.Tests.Verifiers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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

		[TestMethod]
		public async Task When_TypeOf_Included()
		{
			var test = """
				using System;

				namespace Uno
				{
					[NotImplemented("__ANDROID__")]
					public class TestClass
					{
					}
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							_ = [|typeof(Uno.TestClass)|];
						}
					}
				}
				""" + UnoNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "__ANDROID__" });
		}

		[TestMethod]
		public async Task When_MethodReference_Included()
		{
			var test = """
				using System;

				namespace Uno
				{
					public class TestClass
					{
						[NotImplemented("__ANDROID__")]
						public void M()
						{
						}
					}
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var x = new Uno.TestClass();
							Action action = [|x.M|];
							action();
						}
					}
				}
				""" + UnoNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "__ANDROID__" });
		}

		public enum Consumer
		{
			DesktopExe,
			DesktopLibrary,

			/// <summary>
			/// A desktop library as built before UNO_REFERENCE_API/HAS_UNO_SKIA/__UNO_SKIA__ were defined uniformly.
			/// </summary>
			DesktopLibraryWithoutLegacySymbols,

			WasmExe,
			AndroidLibrary,
			IOSLibrary,
			TvOSLibrary,
			NetLibrary,
		}

		// The Uno-relevant part of real 7.0 consumer define sets (MSBuild evaluation of the Uno.WinUI and Uno.Sdk targets).
		private static readonly string[] _netSymbols = ["NET", "NET10_0", "NETCOREAPP", "NET10_0_OR_GREATER"];

		private static readonly string[] _unoSymbols =
		[
			"HAS_UNO", "__UNO__", "HAS_UNO_WINUI", "__UNO_WINUI__", "WINUI_WINDOWING",
			"UNO_HAS_FRAMEWORKELEMENT_MEASUREOVERRIDE", "UNO_HAS_NO_IDEPENDENCYOBJECT",
		];

		private static readonly string[] _legacyUnoSymbols = ["UNO_REFERENCE_API", "HAS_UNO_SKIA", "__UNO_SKIA__"];

		private static string[] GetSymbols(Consumer consumer)
		{
			string[] platform = consumer switch
			{
				Consumer.DesktopExe or Consumer.DesktopLibrary or Consumer.DesktopLibraryWithoutLegacySymbols
					=> ["__DESKTOP__", "DESKTOP", "DISABLE_XAML_GENERATED_BREAK_ON_UNHANDLED_EXCEPTION"],
				Consumer.WasmExe => ["__WASM__", "BROWSERWASM", "DISABLE_XAML_GENERATED_BREAK_ON_UNHANDLED_EXCEPTION"],
				Consumer.AndroidLibrary => ["__ANDROID__", "ANDROID"],
				Consumer.IOSLibrary => ["__IOS__", "IOS", "__MOBILE__", "__UNIFIED__", "__APPLE_UIKIT__"],
				Consumer.TvOSLibrary => ["__TVOS__", "TVOS", "__MOBILE__", "__UNIFIED__", "__APPLE_UIKIT__"],
				_ => [],
			};

			string[] legacy = consumer == Consumer.DesktopLibraryWithoutLegacySymbols ? [] : _legacyUnoSymbols;

			return [.. _netSymbols, .. platform, .. _unoSymbols, .. legacy];
		}

		private const string NotImplementedAttributeSource = """
			#nullable enable
			namespace Uno
			{
				[System.AttributeUsage(System.AttributeTargets.All, Inherited = false, AllowMultiple = false)]
				public sealed class NotImplementedAttribute : System.Attribute
				{
					public NotImplementedAttribute() { }

					public NotImplementedAttribute(params string[] platforms)
					{
						Platforms = platforms;
					}

					public string[]? Platforms { get; }
				}
			}
			""";

		/// <summary>
		/// Declares a stub class in <paramref name="assemblyName"/> and instantiates it from a consumer compiled with
		/// the <paramref name="consumer"/> define set.
		/// </summary>
		/// <param name="tokens">The attribute arguments, or null for a bare <c>[NotImplemented]</c>.</param>
		private static async Task VerifyStubAsync(Consumer consumer, string assemblyName, string[] tokens, bool expected)
		{
			var arguments = tokens is null ? "" : "(" + string.Join(", ", tokens.Select(t => $"\"{t}\"")) + ")";
			var usage = expected ? "[|new Stubs.Stub()|]" : "new Stubs.Stub()";

			var test = new Verify.Test
			{
				TestCode = $$"""
					namespace Consumer
					{
						public class C
						{
							public object M() => {{usage}};
						}
					}
					""",
				PreprocessorSymbols = GetSymbols(consumer),
			};

			var stubSource = $$"""
				namespace Stubs
				{
					[Uno.NotImplemented{{arguments}}]
					public class Stub { }
				}
				""";

			var foundation = new ProjectState("Uno.Foundation", LanguageNames.CSharp, "/0/Foundation", "cs");
			foundation.Sources.Add(NotImplementedAttributeSource);
			test.TestState.AdditionalProjects.Add(foundation.Name, foundation);
			test.TestState.AdditionalProjectReferences.Add(foundation.Name);

			if (assemblyName == foundation.Name)
			{
				foundation.Sources.Add(stubSource);
			}
			else
			{
				var stubs = new ProjectState(assemblyName, LanguageNames.CSharp, "/0/Stubs", "cs");
				stubs.Sources.Add(stubSource);
				stubs.AdditionalProjectReferences.Add(foundation.Name);
				test.TestState.AdditionalProjects.Add(stubs.Name, stubs);
				test.TestState.AdditionalProjectReferences.Add(stubs.Name);
			}

			await test.RunAsync();
		}

		[TestMethod]
		[DataRow(Consumer.DesktopExe, true)]
		[DataRow(Consumer.DesktopLibrary, true)]
		[DataRow(Consumer.DesktopLibraryWithoutLegacySymbols, true)]
		[DataRow(Consumer.WasmExe, true)]
		[DataRow(Consumer.AndroidLibrary, true)]
		[DataRow(Consumer.IOSLibrary, true)]
		[DataRow(Consumer.TvOSLibrary, true)]
		[DataRow(Consumer.NetLibrary, true)]
		public Task When_UnoUI_Skia_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.UI", ["__SKIA__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, true)]
		[DataRow(Consumer.DesktopLibrary, true)]
		[DataRow(Consumer.WasmExe, true)]
		[DataRow(Consumer.AndroidLibrary, true)]
		[DataRow(Consumer.IOSLibrary, true)]
		[DataRow(Consumer.TvOSLibrary, true)]
		[DataRow(Consumer.NetLibrary, true)]
		public Task When_Composition_Skia_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.UI.Composition", ["__SKIA__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, true)]
		[DataRow(Consumer.DesktopLibraryWithoutLegacySymbols, true)]
		[DataRow(Consumer.WasmExe, true)]
		[DataRow(Consumer.AndroidLibrary, true)]
		[DataRow(Consumer.IOSLibrary, true)]
		[DataRow(Consumer.TvOSLibrary, true)]
		[DataRow(Consumer.NetLibrary, true)]
		public Task When_UnoUI_Bare_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.UI", null, expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, false)]
		[DataRow(Consumer.WasmExe, false)]
		[DataRow(Consumer.AndroidLibrary, true)]
		[DataRow(Consumer.IOSLibrary, false)]
		[DataRow(Consumer.NetLibrary, false)]
		public Task When_UnoUI_Consumer_Symbol_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.UI", ["__ANDROID__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, true)]
		[DataRow(Consumer.DesktopLibrary, true)]
		[DataRow(Consumer.DesktopLibraryWithoutLegacySymbols, true)]
		[DataRow(Consumer.WasmExe, true)]
		[DataRow(Consumer.AndroidLibrary, true)]
		[DataRow(Consumer.IOSLibrary, true)]
		[DataRow(Consumer.TvOSLibrary, true)]
		[DataRow(Consumer.NetLibrary, true)]
		public Task When_WinRT_All_Flavors_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.WinRT", ["__ANDROID__", "__IOS__", "__TVOS__", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, true)]
		[DataRow(Consumer.DesktopLibrary, true)]
		[DataRow(Consumer.DesktopLibraryWithoutLegacySymbols, true)]
		[DataRow(Consumer.WasmExe, true)]
		[DataRow(Consumer.AndroidLibrary, false)]
		[DataRow(Consumer.IOSLibrary, true)]
		[DataRow(Consumer.TvOSLibrary, true)]
		[DataRow(Consumer.NetLibrary, true)]
		public Task When_WinRT_Implemented_On_Android_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.WinRT", ["__IOS__", "__TVOS__", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, true)]
		[DataRow(Consumer.DesktopLibrary, true)]
		[DataRow(Consumer.DesktopLibraryWithoutLegacySymbols, true)]
		[DataRow(Consumer.WasmExe, false)]
		[DataRow(Consumer.AndroidLibrary, false)]
		[DataRow(Consumer.IOSLibrary, false)]
		[DataRow(Consumer.TvOSLibrary, true)]
		[DataRow(Consumer.NetLibrary, false)]
		public Task When_WinRT_Desktop_And_TvOS_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.WinRT", ["__TVOS__", "__SKIA__", "__NETSTD_REFERENCE__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, false)]
		[DataRow(Consumer.WasmExe, false)]
		[DataRow(Consumer.AndroidLibrary, false)]
		[DataRow(Consumer.IOSLibrary, false)]
		[DataRow(Consumer.TvOSLibrary, true)]
		[DataRow(Consumer.NetLibrary, false)]
		public Task When_WinRT_TvOS_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.WinRT", ["__TVOS__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, false)]
		[DataRow(Consumer.AndroidLibrary, false)]
		[DataRow(Consumer.IOSLibrary, true)]
		[DataRow(Consumer.TvOSLibrary, true)]
		[DataRow(Consumer.NetLibrary, false)]
		public Task When_WinRT_AppleUIKit_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.WinRT", ["__APPLE_UIKIT__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, true)]
		[DataRow(Consumer.DesktopLibraryWithoutLegacySymbols, true)]
		[DataRow(Consumer.WasmExe, false)]
		[DataRow(Consumer.AndroidLibrary, false)]
		[DataRow(Consumer.NetLibrary, false)]
		public Task When_WinRT_Desktop_Only_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.WinRT", ["__SKIA__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, true)]
		[DataRow(Consumer.WasmExe, true)]
		[DataRow(Consumer.AndroidLibrary, false)]
		[DataRow(Consumer.NetLibrary, true)]
		public Task When_WinRT_Desktop_And_Wasm_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.WinRT", ["__SKIA__", "__WASM__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, false)]
		[DataRow(Consumer.WasmExe, true)]
		[DataRow(Consumer.AndroidLibrary, false)]
		[DataRow(Consumer.NetLibrary, false)]
		public Task When_Foundation_Wasm_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.Foundation", ["__WASM__"], expected);

		[TestMethod]
		[DataRow(Consumer.DesktopExe, false)]
		[DataRow(Consumer.AndroidLibrary, true)]
		[DataRow(Consumer.IOSLibrary, false)]
		[DataRow(Consumer.NetLibrary, false)]
		public Task When_Dispatching_Android_Stub(Consumer consumer, bool expected)
			=> VerifyStubAsync(consumer, "Uno.UI.Dispatching", ["__ANDROID__"], expected);
	}
}
