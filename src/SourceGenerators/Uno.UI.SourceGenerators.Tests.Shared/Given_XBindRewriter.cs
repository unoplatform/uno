using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;

namespace Uno.UI.SourceGenerators.Tests
{
	[TestClass]
	public class Given_XBindRewriter
	{
		[TestMethod]

		// DataTemplates (with context)
		[DataRow("ctx", "MyProperty.A", "ctx.MyProperty.A")]
		[DataRow("ctx", "MyProperty", "ctx.MyProperty")]
		[DataRow("ctx", "MyStaticProperty", "ctx.MyStaticProperty")]
		[DataRow("ctx", "MyStaticMethod()", "ctx.MyStaticMethod()")]
		[DataRow("ctx", "MyProperty.A.ToLower()", "ctx.MyProperty.A.ToLower()")]
		[DataRow("ctx", "global::System.String.Format('{0:X8}', a.Value)", "global::System.String.Format('{0:X8}', ctx.a.Value)")]
		[DataRow("ctx", "Static.MyFunction(42.0)", "ctx.Static.MyFunction(42.0)")]
		[DataRow("ctx", "Static.MyFunction(true)", "ctx.Static.MyFunction(true)")]
		[DataRow("ctx", "Static.MyFunction(MyProperty)", "ctx.Static.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "global::MyNameSpace.Static2.MyProperty", "global::MyNameSpace.Static2.MyProperty")]
		[DataRow("ctx", "global::MyNameSpace.Static2.MyEnum.EnumMember", "global::MyNameSpace.Static2.MyEnum.EnumMember")]
		[DataRow("ctx", "global::MyNameSpace.Static2.MyProperty.ToArray()", "global::MyNameSpace.Static2.MyProperty?.ToArray() ?? global::Windows.UI.Xaml.DependencyProperty.UnsetValue")]
		[DataRow("ctx", "global::MyNameSpace.Static2.MyFunction(MyProperty)", "global::MyNameSpace.Static2.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "MyFunction(MyProperty)", "ctx.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "", "ctx")]

		// Type Casts
		[DataRow("ctx", "(FontFamily)Value", "(FontFamily)ctx.Value")]
		[DataRow("ctx", "(FontFamily)a.Value", "(FontFamily)ctx.a.Value")]
		[DataRow("ctx", "(global::System.Int32)a.Value", "(global::System.Int32)ctx.a.Value")]

		// Pathless casting https://learn.microsoft.com/en-us/windows/uwp/xaml-platform/x-bind-markup-extension#pathless-casting
		[DataRow("ctx", "MyFunction((global::System.String))", "ctx.MyFunction((global::System.String)ctx)")]
		[DataRow("ctx", "MyFunction2((global::System.String),(global::System.String))", "ctx.MyFunction2(((global::System.String)ctx),((global::System.String)ctx))")]
		[DataRow("ctx", "(global::System.String)", "(global::System.String)ctx")]

		// Attached properties
		[DataRow("ctx", "AdornerCanvas.(MyNamespace.FrameworkElementExtensions.ActualWidth)", "MyNamespace.FrameworkElementExtensions.GetActualWidth(ctx.AdornerCanvas)")]
		[DataRow("ctx", "AdornerCanvas.(Grid.Row)", "Grid.GetRow(ctx.AdornerCanvas)")]
		[DataRow("ctx", "global::System.String.Format('{0:X8}', AdornerCanvas.(MyNamespace.FrameworkElementExtensions.ActualWidth))", "global::System.String.Format('{0:X8}', MyNamespace.FrameworkElementExtensions.GetActualWidth(ctx.AdornerCanvas))")]

		// Not supported https://github.com/unoplatform/uno/issues/5061
		[DataRow("ctx", "MyFunction((global::System.Int32)MyProperty)", "ctx.MyFunction((global::System.Int32)ctx.MyProperty)")]
		[DataRow("ctx", "MyFunction((global::System.Int32)MyProperty.Inner)", "ctx.MyFunction((global::System.Int32)ctx.MyProperty.Inner)")]
		[DataRow("ctx", "((MyClass)MyProperty).Test", "((MyClass)ctx.MyProperty).Test")]
		public void When_PathRewrite(string contextName, string inputExpression, string expectedOutput)
		{
			var tree = SyntaxFactory.ParseSyntaxTree("""
				class Context
				{
					public C1 MyProperty { get; }
					public C2 MyNameSpace { get; }
					public static string MyStaticProperty { get; }
					public static string MyStaticMethod() => null;

					public int? a;

					public string MyFunction(float x) => null;
					public string MyFunction(bool x) => null;
					public string MyFunction(C1 x) => null;
					public string MyFunction(string x) => null;
					public string MyFunction(int x) => null;
				}

				class C1
				{
					public string A { get; }
				}

				class C2
				{
					public C3 Static2 { get; }
				}

				class C3
				{
					public string MyProperty { get; }
					public MyEnum MyEnum { get; }
				}

				namespace MyNameSpace
				{
					class Static2
					{
						enum MyEnum
						{
							MyMember,
						}

						class MyList
						{
							public object ToArray() => null;
						}

						public static MyList MyProperty { get; }

						public string MyFunction(MyList x) => null;
					}
				}
				""");
			var compilation = CSharpCompilation.Create(null, new[] { tree });
			var contextSymbol = compilation.GetTypeByMetadataName("Context");
			// TODO: Pass FindType..
			var output = XBindExpressionParser.Rewrite(contextName, inputExpression, contextSymbol, isRValue: true, null!);

			Assert.AreEqual(expectedOutput, output);
		}
	}
}
