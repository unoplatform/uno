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
		public void When_LambdaIsAsyncVoid()
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
					[System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
					public sealed class NotImplementedAttribute : Attribute
					{
		
					}

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
                }";

			var expected = new DiagnosticResult
			{
				Id = UnoNotImplementedAnalyzer.Rule.Id,
				Severity = DiagnosticSeverity.Warning,
				Message = string.Format(UnoNotImplementedAnalyzer.MessageFormat, "Uno.TestClass"),
				Locations = new[] {
					new DiagnosticResultLocation("Test0.cs", 27, 36)
				}
			};

			VerifyDiagnostic(test, expected);
		}
	}
}
