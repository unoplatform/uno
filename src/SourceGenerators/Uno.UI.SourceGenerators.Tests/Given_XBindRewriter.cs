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
		[DataRow("ctx", "MyStaticProperty", "MyStaticProperty")]
		[DataRow("ctx", "MyStaticMethod()", "MyStaticMethod()")]
		[DataRow("ctx", "MyProperty.A.ToLower()", "ctx.MyProperty.A.ToLower()")]
		[DataRow("ctx", "System.String.Format('{0:X8}', a.Value)", "System.String.Format('{0:X8}', ctx.a.Value)")]
		[DataRow("ctx", "Static.MyFunction(42.0)", "Static.MyFunction(42.0)")]
		[DataRow("ctx", "Static.MyFunction(true)", "Static.MyFunction(true)")]
		[DataRow("ctx", "Static.MyFunction(MyProperty)", "Static.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "MyNameSpace.Static2.MyProperty", "MyNameSpace.Static2.MyProperty")]
		[DataRow("ctx", "MyNameSpace.Static2.MyEnum.EnumMember", "MyNameSpace.Static2.MyEnum.EnumMember")]
		[DataRow("ctx", "MyNameSpace.Static2.MyProperty.ToArray()", "MyNameSpace.Static2.MyProperty.ToArray()")]
		[DataRow("ctx", "MyNameSpace.Static2.MyFunction(MyProperty)", "MyNameSpace.Static2.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "MyFunction(MyProperty)", "ctx.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "", "ctx")]
		[DataRow("ctx", "(FontFamily)a.Value", "(FontFamily)ctx.a.Value")]
		[DataRow("ctx", "(global::System.Int32)a.Value", "(global::System.Int32)ctx.a.Value")]

		// Not supported https://github.com/unoplatform/uno/issues/5061
		// [DataRow("ctx", "MyFunction((global::System.Int32)MyProperty)", "ctx.MyFunction((global::System.Int32)ctx.MyProperty)")]

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
			bool IsStaticMethod(string name)
			{
				return name switch
				{
					"MyStaticProperty" => true,
					"MyStaticMethod" => true,
					"Static.MyFunction" => true,
					"System.String.Format" => true,
					"MyNameSpace.Static2.MyFunction" => true,
					"MyNameSpace.Static2.MyProperty" => true,
					"MyNameSpace.Static2.MyEnum" => true,
					_ => false,
				};
			}

			var output = XBindExpressionParser.Rewrite(contextName, inputExpression, IsStaticMethod);

			Assert.AreEqual(expectedOutput, output);
		}
	}
}
