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
		[DataRow("SingleTypeProperty.ToUpper()", "SingleTypeProperty")]
		[DataRow("SingleTypeProperty.A.ToUpper()", "SingleTypeProperty.A")]
		[DataRow("SingleTypeProperty.ToUpper()", "SingleTypeProperty")]
		[DataRow("Static.TestFunction3(TypeProperty1.InnerProp.InnerInnerProp, TypeProperty2.InnerProp)", "TypeProperty1.InnerProp.InnerInnerProp", "TypeProperty2.InnerProp")]
		[DataRow("Static.TestFunction3(TypeProperty1.InnerProp, TypeProperty2)", "TypeProperty1.InnerProp", "TypeProperty2")]
		[DataRow("Static.TestFunction(TypeProperty1)", "TypeProperty1")]
		[DataRow("Static.TestFunction2(TypeProperty1, TypeProperty2)", "TypeProperty1", "TypeProperty2")]
		[DataRow("global::Static.TestFunction2(TypeProperty1)", "TypeProperty1")]
		[DataRow("SingleTypeProperty", "SingleTypeProperty")]
		[DataRow("Max(TypeProperty1, TypeProperty2)", "TypeProperty1", "TypeProperty2")]
		public void When_PathParse(string inputExpression, params string[] output)
		{
			bool IsStaticMethod(string name)
			{
				switch (name)
				{
					case "Static.TestFunction":
					case "Static.TestFunction2":
					case "global::Static.TestFunction2":
					case "Static.TestFunction3":
					case "System.String.Format":
					case "MyNameSpace.Static2.MyFunction":
						return true;
				}

				return false;
			}
			var props = XBindExpressionParser.ParseProperties(inputExpression, IsStaticMethod);

			Assert.IsTrue(output.SequenceEqual(props), $"Expected [{string.Join(";", output)}], got [{string.Join(";", props)}]");
		}
	}
}
