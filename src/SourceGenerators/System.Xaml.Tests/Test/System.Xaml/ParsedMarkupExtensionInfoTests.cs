#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Uno.Xaml.Tests.Test.System.Xaml
{
	[TestFixture]
	public class ParsedMarkupExtensionInfoTests
	{
		public static IEnumerable GetSliceParametersTestCases()
		{
			yield return new TestCaseData("{Binding Path}").Returns(new[] { "Path" });
			yield return new TestCaseData("{Binding Path=Path}").Returns(new[] { "Path=Path" });
			yield return new TestCaseData("{Binding Path, Converter={StaticResource asd}}").Returns(new[]
			{
				"Path",
				"Converter={StaticResource asd}"
			});
			yield return new TestCaseData("{Binding Test={some:NestedExtension With,Multiple,Paremeters}, Prop2='Asd'}").Returns(new[]
			{
				"Test={some:NestedExtension With,Multiple,Paremeters}",
				"Prop2='Asd'"
			});
			yield return new TestCaseData("{Binding Test='text,with,commas,in', Prop1={some:NestedExtension With,Multiple,Paremeters}, Prop2='Asd'}").Returns(new[]
			{
				"Test='text,with,commas,in'",
				"Prop1={some:NestedExtension With,Multiple,Paremeters}",
				"Prop2='Asd'"
			});
			yield return new TestCaseData("{Binding Test1='{}{escaped open bracket', Prop1={some:NestedExtension}, Test2='}close bracket'}").Returns(new[]
			{
				"Test1='{}{escaped open bracket'",
				"Prop1={some:NestedExtension}",
				"Test2='}close bracket'"
			});
			yield return new TestCaseData("{Binding Test1='{', Prop1={some:NestedExtension}, Test2='}close bracket'}").Returns(new[]
			{
				"Test1='{'",
				"Prop1={some:NestedExtension}",
				"Test2='}close bracket'"
			});
			yield return new TestCaseData("{Binding Test1=value without single-quot and with space is legal, Prop2=asd}").Returns(new[]
			{
				"Test1=value without single-quot and with space is legal",
				"Prop2=asd"
			});
			yield return new TestCaseData("{Binding Test1='{}{however to use the {} escape or comma, you need the single-quots}'}").Returns(new[]
			{
				"Test1='{}{however to use the {} escape or comma, you need the single-quots}'"
			});
		}

		[Test]
		[TestCaseSource(nameof(GetSliceParametersTestCases))]
		public string[] SliceParametersTest(string raw)
		{
			// extract only the vargs portion without the starting `{Binding` and the ending `}`
			var vargs = Regex.Match(raw, "^{[^ ]+( (?<vargs>.+))?}$").Groups["vargs"].Value;

			return ParsedMarkupExtensionInfo.SliceParameters(vargs, raw).ToArray();
		}
	}
}
