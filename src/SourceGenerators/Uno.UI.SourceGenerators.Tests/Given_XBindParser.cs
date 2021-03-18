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
		[DataRow("SingleTypeProperty.ToUpper()", true, "SingleTypeProperty")]
		[DataRow("SingleTypeProperty.A.ToUpper()", true, "SingleTypeProperty.A")]
		[DataRow("SingleTypeProperty.ToUpper()", true, "SingleTypeProperty")]
		[DataRow("Static.TestFunction3(TypeProperty1.InnerProp.InnerInnerProp, TypeProperty2.InnerProp)", true, "TypeProperty1.InnerProp.InnerInnerProp", "TypeProperty2.InnerProp")]
		[DataRow("Static.TestFunction3(TypeProperty1.InnerProp, TypeProperty2)", true, "TypeProperty1.InnerProp", "TypeProperty2")]
		[DataRow("Static.TestFunction(TypeProperty1)", true, "TypeProperty1")]
		[DataRow("Static.TestFunction2(TypeProperty1, TypeProperty2)", true, "TypeProperty1", "TypeProperty2")]
		[DataRow("global::Static.TestFunction2(TypeProperty1)", true, "TypeProperty1")]
		[DataRow("SingleTypeProperty", false, "SingleTypeProperty")]
		[DataRow("SingleTypeProperty.Nested", false, "SingleTypeProperty.Nested")]
		[DataRow("Max(TypeProperty1, TypeProperty2)", true, "TypeProperty1", "TypeProperty2")]
		public void When_PathParse(string inputExpression, bool hasFunction, params string[] output)
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

			Assert.AreEqual(props.hasFunction, hasFunction, $"Expected hasFunction=true for [{inputExpression}]");
			Assert.IsTrue(output.SequenceEqual(props.properties), $"Expected [{string.Join(";", output)}], got [{string.Join(";", props)}]");
		}
	}
}
