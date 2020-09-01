using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoslynTester.DiagnosticResults;
using RoslynTester.Helpers;

namespace Uno.Analyzers.Tests
{
	[TestClass]
	public class UnoNotImplementedTests : DiagnosticVerifier
	{
		private static string UnoNotImplementedAtribute = @"
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

		protected override DiagnosticAnalyzer DiagnosticAnalyzer => new UnoNotImplementedAnalyzer();

		public UnoNotImplementedTests() : base(LanguageNames.CSharp)
		{
		}

		[TestMethod]
		public void Nothing()
		{
			var test = @"";

			VerifyDiagnostic(test);
		}

		[TestMethod]
		public void When_EmptyNotImplemented()
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
                           var a = new Uno.TestClass();
                        }
                    }
                }

			" + UnoNotImplementedAtribute;

			var expected = new DiagnosticResult
			{
				Id = UnoNotImplementedAnalyzer.Rule.Id,
				Severity = DiagnosticSeverity.Warning,
				Message = string.Format(UnoNotImplementedAnalyzer.MessageFormat, "Uno.TestClass"),
				Locations = new[] {
					new DiagnosticResultLocation("Test0.cs", 21, 36)
				}
			};

			VerifyDiagnostic(test, expected);
		}

		[TestMethod]
		public void When_SinglePlatform_Included()
		{
			var test = @"
                #define __WASM__

				using System;

				namespace Uno
				{
					[NotImplemented(""__WASM__"")]
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

			" + UnoNotImplementedAtribute;

			var expected = new DiagnosticResult
			{
				Id = UnoNotImplementedAnalyzer.Rule.Id,
				Severity = DiagnosticSeverity.Warning,
				Message = string.Format(UnoNotImplementedAnalyzer.MessageFormat, "Uno.TestClass"),
				Locations = new[] {
					new DiagnosticResultLocation("Test0.cs", 18, 36)
				}
			};

			VerifyDiagnostic(test, expected);
		}

		[TestMethod]
		public void When_SinglePlatform_Excluded()
		{
			var test = @"
                #define __WASM__

				using System;

				namespace Uno
				{
					[NotImplemented(""__SKIA__"")]
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

			" + UnoNotImplementedAtribute;

			VerifyDiagnostic(test);
		}

		[TestMethod]
		public void When_TwoPlatforms_Excluded()
		{
			var test = @"
                #define __WASM__

				using System;

				namespace Uno
				{
					[NotImplemented(""__SKIA__"", ""__IOS__"")]
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

			" + UnoNotImplementedAtribute;

			VerifyDiagnostic(test);
		}

		[TestMethod]
		public void When_Generic_Excluded()
		{
			var test = @"
                #define UNO_REFERENCE_API

				using System;

				namespace Uno
				{
					[NotImplemented(""__IOS__"")]
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

			" + UnoNotImplementedAtribute;

			VerifyDiagnostic(test);
		}

		[TestMethod]
		public void When_Generic_Partial_Excluded()
		{
			var test = @"
                #define UNO_REFERENCE_API

				using System;

				namespace Uno
				{
					[NotImplemented(""__SKIA__"", ""__IOS__"")]
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

			" + UnoNotImplementedAtribute;

			VerifyDiagnostic(test);
		}

		[TestMethod]
		public void When_Generic_Included()
		{
			var test = @"
                #define UNO_REFERENCE_API

				using System;

				namespace Uno
				{
					[NotImplemented(""__SKIA__"", ""__IOS__"", ""__WASM__"")]
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

			" + UnoNotImplementedAtribute;

			var expected = new DiagnosticResult
			{
				Id = UnoNotImplementedAnalyzer.Rule.Id,
				Severity = DiagnosticSeverity.Warning,
				Message = string.Format(UnoNotImplementedAnalyzer.MessageFormat, "Uno.TestClass"),
				Locations = new[] {
					new DiagnosticResultLocation("Test0.cs", 18, 36)
				}
			};

			VerifyDiagnostic(test, expected);
		}


		[TestMethod]
		public void When_Generic_Member_Included()
		{
			var test = @"
                #define UNO_REFERENCE_API

				using System;

				namespace Uno
				{
					public class TestClass {
						[NotImplemented(""__SKIA__"", ""__IOS__"", ""__WASM__"")]
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

			" + UnoNotImplementedAtribute;

			var expected = new DiagnosticResult
			{
				Id = UnoNotImplementedAnalyzer.Rule.Id,
				Severity = DiagnosticSeverity.Warning,
				Message = string.Format(UnoNotImplementedAnalyzer.MessageFormat, "Uno.TestClass.Test"),
				Locations = new[] {
					new DiagnosticResultLocation("Test0.cs", 20, 36)
				}
			};

			VerifyDiagnostic(test, expected);
		}

		[TestMethod]
		public void When_Generic_Member_Partial_Excluded()
		{
			var test = @"
                #define UNO_REFERENCE_API

				using System;

				namespace Uno
				{
					public class TestClass {
						[NotImplemented(""__IOS__"", ""__WASM__"")]
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

			" + UnoNotImplementedAtribute;

			VerifyDiagnostic(test);
		}
	}
}
