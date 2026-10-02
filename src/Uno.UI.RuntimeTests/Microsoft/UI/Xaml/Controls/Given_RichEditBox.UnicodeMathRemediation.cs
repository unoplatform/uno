#nullable enable

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[RunsOnUIThread]
	[DataRow("x_2 ", "msub")]
	[DataRow("x_1^2 ", "msubsup")]
	[DataRow("x^2_1 ", "msubsup")]
	public void When_UnicodeMath_Scripts_Convert_At_A_Space(string input, string expectedElement)
	{
		var editor = CreateUnicodeMathEditor();
		editor.Document.Selection.TypeText(input);

		var math = GetUnicodeMath(editor);
		Assert.IsTrue(math.Descendants().Any(element => element.Name.LocalName == expectedElement));
		Assert.IsFalse(editor.Document.GetRange(0, int.MaxValue).Text.Contains('_'));
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_UnicodeMath_Grouped_Script_Operand_Converts()
	{
		var editor = CreateUnicodeMathEditor();
		editor.Document.Selection.TypeText("x_{i+1} ");

		Assert.IsTrue(GetUnicodeMath(editor).Descendants().Any(element => element.Name.LocalName == "msub"));
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_UnicodeMath_Grouped_And_Nested_Fractions_Convert()
	{
		var editor = CreateUnicodeMathEditor();
		editor.Document.Selection.TypeText("(a+b)/(c/d) ");

		var math = GetUnicodeMath(editor);
		Assert.AreEqual(2, math.Descendants().Count(element => element.Name.LocalName == "mfrac"));
		Assert.IsTrue(math.Descendants().Any(element => element.Name.LocalName == "mfenced"));
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_UnicodeMath_Conversion_Preserves_The_Linear_Undo_Step()
	{
		var editor = CreateUnicodeMathEditor();
		editor.Document.Selection.TypeText("x_{i+1}^2 ");
		Assert.IsTrue(GetUnicodeMath(editor).Descendants().Any(element => element.Name.LocalName == "msubsup"));

		editor.Document.Undo();

		Assert.AreEqual("\U0001D465_{\U0001D456+1}^2\r", editor.Document.GetRange(0, int.MaxValue).Text);
		Assert.IsFalse(GetUnicodeMath(editor).Descendants().Any(element => element.Name.LocalName == "msubsup"));
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_UnicodeMath_Does_Not_Convert_Before_A_Boundary()
	{
		var editor = CreateUnicodeMathEditor();
		editor.Document.Selection.TypeText("x_{i+1}^2");

		Assert.IsFalse(GetUnicodeMath(editor).Descendants().Any(element => element.Name.LocalName == "msubsup"));
		Assert.AreEqual("\U0001D465_{\U0001D456+1}^2\r", editor.Document.GetRange(0, int.MaxValue).Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	// This exercises Uno's managed UnicodeMath parser; native RichEdit does not preserve these inputs linearly.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_UnicodeMath_Ordinary_Or_Unbalanced_Text_Remains_Linear()
	{
		foreach (var input in new[] { "ordinary text ", "x_( ", "/a ", "a//b ", "/ " })
		{
			var editor = CreateUnicodeMathEditor();
			editor.Document.Selection.TypeText(input);

			var math = GetUnicodeMath(editor);
			Assert.IsFalse(math.Descendants().Any(element =>
				element.Name.LocalName is "mfrac" or "msub" or "msup" or "msubsup" or "mfenced" or "msqrt" or "mroot"));
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_UnicodeMath_Radicals_Commands_And_Aliases_Convert()
	{
		var editor = CreateUnicodeMathEditor();
		editor.Document.Selection.TypeText(@"\sqrt{a/b} ");
		var squareRoot = GetUnicodeMath(editor);
		Assert.IsTrue(squareRoot.Descendants().Any(element => element.Name.LocalName == "msqrt"));
		Assert.IsTrue(squareRoot.Descendants().Any(element => element.Name.LocalName == "mfrac"));

		editor = CreateUnicodeMathEditor();
		editor.Document.Selection.TypeText(@"\root{3}{x_1} ");
		var root = GetUnicodeMath(editor);
		Assert.IsTrue(root.Descendants().Any(element => element.Name.LocalName == "mroot"));
		Assert.IsTrue(root.Descendants().Any(element => element.Name.LocalName == "msub"));

		editor = CreateUnicodeMathEditor();
		editor.Document.Selection.TypeText(@"\alpha+\beta\le\infty ");
		var commandText = string.Concat(GetUnicodeMath(editor).DescendantNodes().OfType<XText>().Select(text => text.Value));
		StringAssert.Contains(commandText, "α");
		StringAssert.Contains(commandText, "β");
		StringAssert.Contains(commandText, "≤");
		StringAssert.Contains(commandText, "∞");
	}

	private static RichEditBox CreateUnicodeMathEditor()
	{
		var editor = new RichEditBox();
		editor.Document.SetMathMode(RichEditMathMode.MathOnly);
		return editor;
	}

	private static XElement GetUnicodeMath(RichEditBox editor)
	{
		editor.Document.GetMathML(out var value);
		return XDocument.Parse(value).Root!;
	}
}
