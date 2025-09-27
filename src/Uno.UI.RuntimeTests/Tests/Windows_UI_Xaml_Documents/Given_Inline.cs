using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions.Execution;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

[TestClass]
public partial class Given_Inline // compiled xaml
{
	[TestMethod]
	[RunsOnUIThread]
	public void Compiled_XmlSpace_Preserve_Parsing()
	{
		var setup = new Setup.InlineTextLiteralTests().XmlSpacePreserveBlock;

		Assert.AreEqual(setup.Tag, setup.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void Compiled_TextBlock_Inline_Parsing()
	{
		var setup = new Setup.InlineTextLiteralTests().TestPanel;

		AssertParsingResults(setup.Children.OfType<TextBlock>(), x => x.Name, x => x.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void Compiled_Inline_Run_Parsing()
	{
		var setup = new Setup.InlineTextLiteralTests().RunTextBlock;
		var controls = setup.Inlines.OfType<Run>()
			// filter out the single-space runs that are inserted between every node of inline nodes
			// they are not present in the xaml, but injected by the parsing engine
			.Where(x => !(string.IsNullOrEmpty(x.Name) && x.Text == " "));

		AssertParsingResults(controls, x => x.Name, x => x.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void Compiled_Inline_Bold_Parsing()
	{
		var setup = new Setup.InlineTextLiteralTests().InlineTextBlock;

		AssertParsingResults(setup.Inlines.OfType<Bold>(), x => x.Name, x => ((Run)x.Inlines.Single()).Text);
	}
}

public partial class Given_Inline // dynamic xaml (XamlReader)
{
	[TestMethod]
	[RunsOnUIThread]
	public void Dynamic_TextBlock_Inline_Parsing()
	{
		var setup = XamlHelper.LoadXaml<StackPanel>("""
			<StackPanel x:Name="TestPanel">
				<TextBlock x:Name="T01" Text="TextBlock/@Text" />
				<TextBlock x:Name="T02" Text=" TextBlock/@Text" />
				<TextBlock x:Name="T03" Text=" TextBlock/@Text " />
				<TextBlock x:Name="T04" Text="  TextBlock/@Text  " />

				<TextBlock x:Name="T11">TextBlock/Text()</TextBlock>
				<TextBlock x:Name="T12"> TextBlock/Text()</TextBlock>
				<TextBlock x:Name="T13"> TextBlock/Text() </TextBlock>
				<TextBlock x:Name="T14">  TextBlock/Text()  </TextBlock>

				<TextBlock x:Name="T21">
					<TextBlock.Text>TextBlock/TextBlock.Text/Text()</TextBlock.Text>
				</TextBlock>
				<TextBlock x:Name="T22">
					<TextBlock.Text> TextBlock/TextBlock.Text/Text()</TextBlock.Text>
				</TextBlock>
				<TextBlock x:Name="T23">
					<TextBlock.Text> TextBlock/TextBlock.Text/Text() </TextBlock.Text>
				</TextBlock>
				<TextBlock x:Name="T24">
					<TextBlock.Text>  TextBlock/TextBlock.Text/Text()  </TextBlock.Text>
				</TextBlock>

				<TextBlock x:Name="T31">
					TextBlock/Text()$multi-line
				</TextBlock>
				<TextBlock x:Name="T32">
					TextBlock/Text()$multi-line1
					TextBlock/Text()$multi-line2
				</TextBlock>
				<TextBlock x:Name="T41">
					<TextBlock.Text>
						TextBlock/TextBlock.Text/Text()$multi-line
					</TextBlock.Text>
				</TextBlock>
				<TextBlock x:Name="T42">
					<TextBlock.Text>
						TextBlock/TextBlock.Text/Text()$multi-line1
						TextBlock/TextBlock.Text/Text()$multi-line2
					</TextBlock.Text>
				</TextBlock>
			</StackPanel>
		""");

		AssertParsingResults(setup.Children.OfType<TextBlock>(), x => x.Name, x => x.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void Dynamic_Inline_Run_Parsing()
	{
		var setup = XamlHelper.LoadXaml<TextBlock>("""
			<TextBlock x:Name="RunTextBlock">
				<Run x:Name="R01" Text="Run/@Text" />
				<Run x:Name="R02" Text=" Run/@Text" />
				<Run x:Name="R03" Text=" Run/@Text " />
				<Run x:Name="R04" Text="  Run/@Text  " />

				<Run x:Name="R11">Run/Text()</Run>
				<Run x:Name="R12"> Run/Text()</Run>
				<Run x:Name="R13"> Run/Text() </Run>
				<Run x:Name="R14">  Run/Text()  </Run>

				<Run x:Name="R21">
					<Run.Text>Run/Run.Text/Text()</Run.Text>
				</Run>
				<Run x:Name="R22">
					<Run.Text> Run/Run.Text/Text()</Run.Text>
				</Run>
				<Run x:Name="R23">
					<Run.Text> Run/Run.Text/Text() </Run.Text>
				</Run>
				<Run x:Name="R24">
					<Run.Text>  Run/Run.Text/Text()  </Run.Text>
				</Run>

				<Run x:Name="R31">
					Run/Text()$multi-line
				</Run>
				<Run x:Name="R32">
					Run/Text()$multi-line1
					Run/Text()$multi-line2
				</Run>
				<Run x:Name="R41">
					<Run.Text>
						Run/Run.Text/Text()$multi-line
					</Run.Text>
				</Run>
				<Run x:Name="R42">
					<Run.Text>
						Run/Run.Text/Text()$multi-line1
						Run/Run.Text/Text()$multi-line2
					</Run.Text>
				</Run>
			</TextBlock>
		""");
		var controls = setup.Inlines.OfType<Run>()
			// filter out the single-space runs that are inserted between every node of inline nodes
			// they are not present in the xaml, but injected by the parsing engine
			.Where(x => !(string.IsNullOrEmpty(x.Name) && x.Text == " "));

		AssertParsingResults(controls, x => x.Name, x => x.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void Dynamic_Inline_Bold_Parsing()
	{
		var setup = XamlHelper.LoadXaml<TextBlock>("""
			<TextBlock x:Name="InlineTextBlock">
				<Bold x:Name="B11">Bold/Text()</Bold>
				<Bold x:Name="B12"> Bold/Text()</Bold>
				<Bold x:Name="B13"> Bold/Text() </Bold>
				<Bold x:Name="B14">  Bold/Text()  </Bold>

				<Bold x:Name="B31">
					Bold/Text()$multi-line
				</Bold>
				<Bold x:Name="B32">
					Bold/Text()$multi-line1
					Bold/Text()$multi-line2
				</Bold>
			</TextBlock>
		""");

		AssertParsingResults(setup.Inlines.OfType<Bold>(), x => x.Name, x => ((Run)x.Inlines.Single()).Text);
	}
}

public partial class Given_Inline // helper
{
	public record InlineTextExpectation(string Name, string Value);

	private static void AssertParsingResults<T>(IEnumerable<T> controls, Func<T, string> keySelector, Func<T, string> valueSelector)
	{
		var typename = typeof(T).Name;

		var targets = controls.ToDictionary(keySelector, valueSelector);
		var testcases = GetExpectations(typename).ToDictionary(x => x.Name, x => x);

		CollectionAssert.AreEqual(
			testcases.Keys,
			targets.Keys,
			"Test targets names not matching the expected test cases names: " + string.Join(",", Enumerable.Concat(
				testcases.Keys.Except(targets.Keys).Select(x => $"-{x}"), // missing
				targets.Keys.Except(testcases.Keys).Select(x => $"+{x}") // extra
			)));

		using var _ = new AssertionScope();
		foreach (var key in testcases.Keys)
		{
			Assert.AreEqual(testcases[key].Value, targets[key], $"{typename}#{key}");
		}
	}

	private static string FormatOneline(string s) => s
		.Replace("\n", "\\n")
		.Replace("\r", "\\r")
		.Replace("\t", "\\t");

	private static IEnumerable<InlineTextExpectation> GetExpectations(string typename)
	{
		var hasTextProperty = typename is "TextBlock" or "Run";
		var prefix = typename[0];

		// padding is only preserved for attributes (scenarios X0)
		// for text-literal, padding are trimmed, and one space is added between each line (scenarios X1-4)

		if (hasTextProperty)
		{
			yield return new($"{prefix}01", $"{typename}/@Text");
			yield return new($"{prefix}02", $" {typename}/@Text");
			yield return new($"{prefix}03", $" {typename}/@Text ");
			yield return new($"{prefix}04", $"  {typename}/@Text  ");
		}

		for (int i = 0; i < 4; i++)
		{
			yield return new($"{prefix}1{i + 1}", $"{typename}/Text()");
		}

		if (hasTextProperty)
		{
			for (int i = 0; i < 4; i++)
			{
				yield return new($"{prefix}2{i + 1}", $"{typename}/{typename}.Text/Text()");
			}
		}

		yield return new($"{prefix}31", $"{typename}/Text()$multi-line");
		yield return new($"{prefix}32", $"{typename}/Text()$multi-line1 {typename}/Text()$multi-line2");

		if (hasTextProperty)
		{
			yield return new($"{prefix}41", $"{typename}/{typename}.Text/Text()$multi-line");
			yield return new($"{prefix}42", $"{typename}/{typename}.Text/Text()$multi-line1 {typename}/{typename}.Text/Text()$multi-line2");
		}
	}
}
