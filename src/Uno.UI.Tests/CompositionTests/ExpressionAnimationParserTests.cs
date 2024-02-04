using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.CompositionTests;

[TestClass]
public class ExpressionAnimationParserTests
{
	public class MyClass : CompositionObject
	{
		public Vector2 Offset => new Vector2(5);
	}

	[TestMethod]
	public void TestUnaryMinusExpressionWithMemberAccess()
	{
		var expressionAnimation = new ExpressionAnimation(null);
		expressionAnimation.SetReferenceParameter("test", new MyClass());
		var parser = new ExpressionAnimationParser("-test.Offset.X");
		var expression = parser.Parse();
		var result = expression.Evaluate(expressionAnimation);
		Assert.AreEqual(-5.0f, result);
	}
}
