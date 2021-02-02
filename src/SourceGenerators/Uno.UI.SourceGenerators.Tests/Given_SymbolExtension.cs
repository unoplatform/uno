using Microsoft.Build.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;

namespace Uno.UI.SourceGenerators.Tests
{
	[TestClass]
	public class Given_SymbolExtension
	{
		[TestMethod]
		[DataRow("int", "int")]
		[DataRow("double", "double")]
		[DataRow("System.Collections.Generic.IList<int>", "global::System.Collections.Generic.IList<int>")]
		[DataRow("System.Collections.Generic.IList<System.Disposable>", "global::System.Collections.Generic.IList<global::System.Disposable>")]
		[DataRow("System.Collections.Generic.IDictionary<int, System.Disposable>", "global::System.Collections.Generic.IDictionary<int, global::System.Disposable>")]
		[DataRow("(int, int)", "(int, int)")]
		[DataRow("(int, System.Disposable)", "(int, global::System.Disposable)")]
		[DataRow("System.Collections.Generic.IList<(int, System.Disposable)>", "global::System.Collections.Generic.IList<(int, global::System.Disposable)>")]
		[DataRow("System.Collections.Generic.IList<(string, string, double)>", "global::System.Collections.Generic.IList<(string, string, double)>")]
		public void When_GetFullyQualifiedString(string input, string expected)
		{
			var compilation = CreateTestCompilation(input);

			if (compilation.GetTypeByMetadataName("Test") is INamedTypeSymbol testSymbol)
			{
				if (testSymbol.GetAllMembersWithName("_myField").FirstOrDefault() is IPropertySymbol myField)
				{
					var actual = myField.Type.GetFullyQualifiedType();
					Assert.AreEqual<string>(expected, actual);
				}
				else
				{
					Assert.Fail();
				}
			}
			else
			{
				Assert.Fail();
			}
		}

		private static Compilation CreateTestCompilation(string type)
		{
			var programPath = @"Program.cs";
			var programText = $"public class Test {{ public static {type} _myField {{ get; set; }} }}";
			var programTree = CSharpSyntaxTree
				.ParseText(programText)
				.WithFilePath(programPath);

			SyntaxTree[] sourceTrees = { programTree };

			MetadataReference mscorlib = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
			MetadataReference codeAnalysis = MetadataReference.CreateFromFile(typeof(SyntaxTree).GetTypeInfo().Assembly.Location);
			MetadataReference csharpCodeAnalysis = MetadataReference.CreateFromFile(typeof(CSharpSyntaxTree).GetTypeInfo().Assembly.Location);
			MetadataReference[] references = { mscorlib, codeAnalysis, csharpCodeAnalysis };

			return CSharpCompilation.Create("ConsoleApplication",
							 sourceTrees,
							 references,
							 new CSharpCompilationOptions(OutputKind.ConsoleApplication));
		}
	}
}
