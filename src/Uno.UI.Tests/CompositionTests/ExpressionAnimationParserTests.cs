using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.CompositionTests;

[TestClass]
public class ExpressionAnimationParserTests
{
	[TestMethod]
	public void TestUnaryMinusExpressionWithMemberAccess()
	{
		var compositor = Compositor.GetSharedCompositor();
		var expressionAnimation = compositor.CreateExpressionAnimation("-test.Offset.X");
		var visual = compositor.CreateShapeVisual();
		visual.Offset = new Vector3(5, -10, 0);
		expressionAnimation.SetReferenceParameter("test", visual);
		var parser = new ExpressionAnimationParser(expressionAnimation.Expression);
		var expression = parser.Parse();
		var result = expression.Evaluate(expressionAnimation);
		Assert.AreEqual(-5.0f, result);
	}

	[TestMethod]
	public void TestTernaryExpression_Simple_When_True()
	{
		var compositor = Compositor.GetSharedCompositor();
		var expressionAnimation = compositor.CreateExpressionAnimation("true ? 5 : 4");
		var parser = new ExpressionAnimationParser(expressionAnimation.Expression);
		var expression = parser.Parse();
		var result = expression.Evaluate(expressionAnimation);
		Assert.AreEqual(5.0f, result);
	}

	[TestMethod]
	public void TestTernaryExpression_Simple_When_False()
	{
		var compositor = Compositor.GetSharedCompositor();
		var expressionAnimation = compositor.CreateExpressionAnimation("false ? 5 : 4");
		var parser = new ExpressionAnimationParser(expressionAnimation.Expression);
		var expression = parser.Parse();
		var result = expression.Evaluate(expressionAnimation);
		Assert.AreEqual(4.0f, result);
	}

	[TestMethod]
	public void TestTernaryExpression_Condition_Is_Binary_When_True()
	{
		var compositor = Compositor.GetSharedCompositor();
		var expressionAnimation = compositor.CreateExpressionAnimation("5 > 4 ? 8.0f : 9.0f");
		var parser = new ExpressionAnimationParser(expressionAnimation.Expression);
		var expression = parser.Parse();
		var result = expression.Evaluate(expressionAnimation);
		Assert.AreEqual(8.0f, result);
	}

	[TestMethod]
	public void TestTernaryExpression_Condition_Is_Binary_When_False()
	{
		var compositor = Compositor.GetSharedCompositor();
		var expressionAnimation = compositor.CreateExpressionAnimation("5 <= 4 ? 8.0f : 9.0f");
		var parser = new ExpressionAnimationParser(expressionAnimation.Expression);
		var expression = parser.Parse();
		var result = expression.Evaluate(expressionAnimation);
		Assert.AreEqual(9.0f, result);
	}
}
