using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
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
		[DataRow("ctx", "MyNameSpace.Static2.MyProperty", "ctx.MyNameSpace.Static2.MyProperty")]
		[DataRow("ctx", "MyNameSpace.Static2.MyEnum.EnumMember", "ctx.MyNameSpace.Static2.MyEnum.EnumMember")]
		[DataRow("ctx", "MyNameSpace.Static2.MyProperty.ToArray()", "ctx.MyNameSpace.Static2.MyProperty.ToArray()")]
		[DataRow("ctx", "MyNameSpace.Static2.MyFunction(MyProperty)", "ctx.MyNameSpace.Static2.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "MyFunction(MyProperty)", "ctx.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "", "ctx")]

		// Type Casts
		[DataRow("ctx", "(FontFamily)Value", "(FontFamily)ctx.Value")]
		[DataRow("ctx", "(FontFamily)a.Value", "(FontFamily)ctx.a.Value")]
		[DataRow("ctx", "(global::System.Int32)a.Value", "(global::System.Int32)ctx.a.Value")]

		// Pathless casting https://learn.microsoft.com/en-us/windows/uwp/xaml-platform/x-bind-markup-extension#pathless-casting
		[DataRow("ctx", "MyFunction((global::System.String))", "ctx.MyFunction(((global::System.String)ctx))")]
		[DataRow("ctx", "MyFunction2((global::System.String),(global::System.String))", "ctx.MyFunction2(((global::System.String)ctx),((global::System.String)ctx))")]
		[DataRow("ctx", "(global::System.String)", "((global::System.String)ctx)")]

		// Attached properties
		[DataRow("ctx", "AdornerCanvas.(MyNamespace.FrameworkElementExtensions.ActualWidth)", "MyNamespace.FrameworkElementExtensions.GetActualWidth(ctx.AdornerCanvas)")]
		[DataRow("ctx", "AdornerCanvas.(Grid.Row)", "Grid.GetRow(ctx.AdornerCanvas)")]
		[DataRow("ctx", "global::System.String.Format('{0:X8}', AdornerCanvas.(MyNamespace.FrameworkElementExtensions.ActualWidth))", "global::System.String.Format('{0:X8}', MyNamespace.FrameworkElementExtensions.GetActualWidth(ctx.AdornerCanvas))")]

		// Not supported https://github.com/unoplatform/uno/issues/5061
		[DataRow("ctx", "MyFunction((global::System.Int32)MyProperty)", "ctx.MyFunction((global::System.Int32)ctx.MyProperty)")]
		[DataRow("ctx", "MyFunction((global::System.Int32)MyProperty.Inner)", "ctx.MyFunction((global::System.Int32)ctx.MyProperty.Inner)")]
		[DataRow("ctx", "((MyClass)MyProperty).Test", "((MyClass)ctx.MyProperty).Test")]

		// Main class (without context)
		[DataRow("", "MyProperty.A", "MyProperty.A")]
		[DataRow("", "MyProperty", "MyProperty")]
		[DataRow("", "MyProperty.A.ToLower()", "MyProperty.A.ToLower()")]
		[DataRow("", "System.String.Format('{0:X8}', a.Value)", "System.String.Format('{0:X8}', a.Value)")]
		[DataRow("", "Static.MyFunction(42.0)", "Static.MyFunction(42.0)")]
		[DataRow("", "Static.MyFunction(true)", "Static.MyFunction(true)")]
		[DataRow("", "Static.MyFunction(MyProperty)", "Static.MyFunction(MyProperty)")]
		[DataRow("", "MyNameSpace.Static2.MyFunction(MyProperty)", "MyNameSpace.Static2.MyFunction(MyProperty)")]
		[DataRow("", "MyFunction(MyProperty)", "MyFunction(MyProperty)")]
		[DataRow("", "(FontFamily)MyProperty", "(FontFamily)MyProperty")]
		[DataRow("", "(FontFamily)MyProperty.A", "(FontFamily)MyProperty.A")]
		public void When_PathRewrite(string contextName, string inputExpression, string expectedOutput)
		{
			var output = XBindExpressionParser.Rewrite(contextName, inputExpression);

			Assert.AreEqual(expectedOutput, output);
		}
	}
}
