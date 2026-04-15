using NUnit.Framework;
using Uno.Xaml;

namespace Uno.Xaml.Tests.Test.System.Xaml
{
	/// <summary>
	/// Uno feature 004 (XAML C# expressions) — T035a: locks in the parser-bypass
	/// heuristic in <see cref="XamlXmlParser.IsUnoCSharpExpressionAttribute"/>.
	/// The method decides which <c>{…}</c> attribute values are NEVER valid markup
	/// extensions and therefore must skip <c>ParsedMarkupExtensionInfo.Parse</c> so
	/// the downstream generator classifier receives the raw text.
	/// </summary>
	[TestFixture]
	public class IsUnoCSharpExpressionAttributeTests
	{
		// --- Directive forms (T015 baseline) -----------------------------------

		[TestCase("{= Foo}")]
		[TestCase("{=Foo+Bar}")]
		[TestCase("{.Member}")]
		[TestCase("{.Foo.Bar}")]
		[TestCase("{this.WindowTitle}")]
		[TestCase("{this.User.Address.City}")]
		public void DirectiveForms_AreBypassed(string value)
			=> Assert.IsTrue(XamlXmlParser.IsUnoCSharpExpressionAttribute(value));

		// --- Unambiguous expression shapes (T035a extension) -------------------

		[TestCase("{FirstName + LastName}")]
		[TestCase("{Price * Quantity}")]
		[TestCase("{Balance / 2}")]
		[TestCase("{Count % 3}")]
		[TestCase("{A ^ B}")]
		[TestCase("{~A}")]
		public void ArithmeticOperators_AreBypassed(string value)
			=> Assert.IsTrue(XamlXmlParser.IsUnoCSharpExpressionAttribute(value));

		[TestCase("{Count > 0}")]
		[TestCase("{Count < 10}")]
		[TestCase("{!IsEnabled}")]
		[TestCase("{Foo == Bar}")]
		public void ComparisonOperators_AreBypassed(string value)
			=> Assert.IsTrue(XamlXmlParser.IsUnoCSharpExpressionAttribute(value));

		[TestCase("{IsEnabled && IsVisible}")]
		[TestCase("{A || B}")]
		public void LogicalOperators_AreBypassed(string value)
			=> Assert.IsTrue(XamlXmlParser.IsUnoCSharpExpressionAttribute(value));

		[TestCase("{Nickname ?? 'Anonymous'}")]
		[TestCase("{IsVip ? 'Gold' : 'Standard'}")]
		public void NullCoalescingAndTernary_AreBypassed(string value)
			=> Assert.IsTrue(XamlXmlParser.IsUnoCSharpExpressionAttribute(value));

		[TestCase("{$'{Balance:C2}'}")]
		[TestCase("{$\"hello {Name}\"}")]
		public void InterpolatedStrings_AreBypassed(string value)
			=> Assert.IsTrue(XamlXmlParser.IsUnoCSharpExpressionAttribute(value));

		[TestCase("{(s, e) => Counter++}")]
		[TestCase("{() => Close()}")]
		public void LambdaIntroducer_IsBypassed(string value)
			=> Assert.IsTrue(XamlXmlParser.IsUnoCSharpExpressionAttribute(value));

		// --- Preserved markup-extension shapes --------------------------------

		[TestCase("{Binding Foo}")]
		[TestCase("{Binding Foo, Mode=TwoWay}")]
		[TestCase("{Binding Foo?.Bar}")]
		[TestCase("{x:Bind Foo?.Bar}")]
		[TestCase("{StaticResource MyBrush}")]
		[TestCase("{ThemeResource PaneBackground}")]
		[TestCase("{x:Null}")]
		[TestCase("{TemplateBinding Text}")]
		[TestCase("{FirstName}")] // bare identifier: still ambiguous with FirstNameExtension
		[TestCase("{Foo.Bar}")] // dotted identifier: still ambiguous
		[TestCase("{StaticResource '0.00%'}")] // quote content must NOT be scanned for operators
		[TestCase("{Binding Path, ConverterParameter='a+b*c'}")] // ditto
		public void KnownMarkupExtensions_AreNotBypassed(string value)
			=> Assert.IsFalse(XamlXmlParser.IsUnoCSharpExpressionAttribute(value));

		// --- Defensive guards --------------------------------------------------

		[TestCase("")]
		[TestCase("{}")]
		[TestCase("{")]
		[TestCase("Not a markup extension")]
		public void MalformedOrEmpty_AreNotBypassed(string value)
			=> Assert.IsFalse(XamlXmlParser.IsUnoCSharpExpressionAttribute(value));
	}
}
