using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;

namespace Uno.UI.SourceGenerators.Tests
{
	[TestClass]
	public class Given_XBindParser
	{
		[TestMethod]
		[DataRow("Static.TestFunction3(TypeProperty1.InnerProp.InnerInnerProp, TypeProperty2.InnerProp)", "TypeProperty1.InnerProp.InnerInnerProp", "TypeProperty2.InnerProp")]
		[DataRow("Static.TestFunction3(TypeProperty1.InnerProp, TypeProperty2)", "TypeProperty1.InnerProp", "TypeProperty2")]
		[DataRow("Static.TestFunction(TypeProperty1)", "TypeProperty1")]
		[DataRow("Static.TestFunction2(TypeProperty1, TypeProperty2)", "TypeProperty1", "TypeProperty2")]
		[DataRow("global::Static.TestFunction2(TypeProperty1)", "TypeProperty1")]
		[DataRow("SingleTypeProperty", "SingleTypeProperty")]
		[DataRow("Max(TypeProperty1, TypeProperty2)", "TypeProperty1", "TypeProperty2")]
		public void When_PathParse(string inputExpression, params string[] output)
		{
			var props = XBindExpressionParser.ParseProperties(inputExpression);

			Assert.IsTrue(output.SequenceEqual(props), $"Expected [{string.Join(";", output)}], got [{string.Join(";", props)}]");
		}
	}
}
