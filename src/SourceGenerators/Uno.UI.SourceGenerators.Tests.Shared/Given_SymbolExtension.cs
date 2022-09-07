using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uno.Extensions;
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

		[TestMethod]
		public void When_Generating_Nested_Class()
		{
			var compilation = CreateCompilationWithProgramText(@"
namespace A.B
{
    namespace C
    {
        partial class D
        {
            partial class E
            {
            }
        }
    }
}");
			var type = compilation.GetTypeByMetadataName("A.B.C.D+E");
			Assert.IsNotNull(type);
			var builder = new IndentedStringBuilder();
			var disposables = type.AddToIndentedStringBuilder(builder);
			Assert.AreEqual(@"namespace A.B.C
{
	partial class D
	{
		partial class E
		{
", builder.ToString());

			while (disposables.Count > 0)
			{
				disposables.Pop().Dispose();
			}

			Assert.AreEqual(@"namespace A.B.C
{
	partial class D
	{
		partial class E
		{
		}
	}
}
", builder.ToString());
		}

		[TestMethod]
		public void When_Generating_Nested_Class_With_Action()
		{
			var compilation = CreateCompilationWithProgramText(@"
namespace A.B
{
    namespace C
    {
        partial class D
        {
            partial struct E
            {
            }
        }
    }
}");
			var type = compilation.GetTypeByMetadataName("A.B.C.D+E");
			Assert.IsNotNull(type);
			var builder = new IndentedStringBuilder();
			var disposables = type.AddToIndentedStringBuilder(builder, builder => builder.AppendLineIndented("[MyAttribute]"));
			Assert.AreEqual(@"namespace A.B.C
{
	partial class D
	{
		[MyAttribute]
		partial struct E
		{
", builder.ToString());

			while (disposables.Count > 0)
			{
				disposables.Pop().Dispose();
			}

			Assert.AreEqual(@"namespace A.B.C
{
	partial class D
	{
		[MyAttribute]
		partial struct E
		{
		}
	}
}
", builder.ToString());
		}

		[TestMethod]
		public void When_Generating_Generic_Type()
		{
			var compilation = CreateCompilationWithProgramText(@"
class C<T1, T2>
{
}");
			var type = compilation.GetTypeByMetadataName("C`2");
			Assert.IsNotNull(type);
			var builder = new IndentedStringBuilder();
			var disposables = type.AddToIndentedStringBuilder(builder);
			while (disposables.Count > 0)
			{
				disposables.Pop().Dispose();
			}

			Assert.AreEqual(@"partial class C<T1, T2>
{
}
", builder.ToString());
		}

		[TestMethod]
		public void When_Generating_Generic_Type_With_Variance()
		{
			var compilation = CreateCompilationWithProgramText(@"
interface I<in T1, out T2, T3>
{
	class C { }
}");
			var type = compilation.GetTypeByMetadataName("I`3+C");
			Assert.IsNotNull(type);
			var builder = new IndentedStringBuilder();
			var disposables = type.AddToIndentedStringBuilder(builder);
			while (disposables.Count > 0)
			{
				disposables.Pop().Dispose();
			}

			Assert.AreEqual(@"partial interface I<in T1, out T2, T3>
{
	partial class C
	{
	}
}
", builder.ToString());
		}

		private static Compilation CreateTestCompilation(string type)
			=> CreateCompilationWithProgramText($"public class Test {{ public static {type} _myField {{ get; set; }} }}");

		private static Compilation CreateCompilationWithProgramText(string text)
		{
			var programPath = @"Program.cs";
			var programText = text;
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
