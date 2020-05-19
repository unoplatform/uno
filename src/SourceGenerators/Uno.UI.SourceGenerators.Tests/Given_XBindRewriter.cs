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
		[DataRow("ctx", "MyProperty.A.ToLower()", "ctx.MyProperty.A.ToLower()")]
		[DataRow("ctx", "System.String.Format('{0:X8}', a.Value)", "System.String.Format('{0:X8}', ctx.a.Value)")]
		[DataRow("ctx", "Static.MyFunction(42.0)", "Static.MyFunction(42.0)")]
		[DataRow("ctx", "Static.MyFunction(true)", "Static.MyFunction(true)")]
		[DataRow("ctx", "Static.MyFunction(MyProperty)", "Static.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "MyNameSpace.Static2.MyProperty", "MyNameSpace.Static2.MyProperty")]
		[DataRow("ctx", "MyNameSpace.Static2.MyFunction(MyProperty)", "MyNameSpace.Static2.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "MyFunction(MyProperty)", "ctx.MyFunction(ctx.MyProperty)")]
		[DataRow("ctx", "", "ctx")]

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
		public void When_PathRewrite(string contextName, string inputExpression, string expectedOutput)
		{
			bool IsStaticMethod(string name)
			{
				switch (name)
				{
					case "Static.MyFunction":
						return true;
					case "System.String.Format":
						return true;
					case "MyNameSpace.Static2.MyFunction":
						return true;
					case "MyNameSpace.Static2.MyProperty":
						return true;
				}

				return false;
			}

			var output = XBindExpressionParser.Rewrite(contextName, inputExpression, IsStaticMethod);

			Assert.AreEqual(expectedOutput, output);
		}
	}
}
