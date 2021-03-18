#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;

namespace Uno.UI.SourceGenerators.Tests
{
	[TestClass]
	public class Given_xBindPathParser
	{
		[TestMethod]

		[DataRow("<ListView ItemsSource=\"{x:Bind Model.Items, Mode=OneWay}\" SelectedItem=\"{x:Bind Model.SelectedItem, Mode=TwoWay}\">", "<ListView ItemsSource=\"{x:Bind TQBvAGQAZQBsAC4ASQB0AGUAbQBzAA__, Mode=OneWay}\" SelectedItem=\"{x:Bind TQBvAGQAZQBsAC4AUwBlAGwAZQBjAHQAZQBkAEkAdABlAG0A, Mode=TwoWay}\">")]

		// unnamed first parameter
		[DataRow("\"{x:Bind}\"", "\"{x:Bind}\"")]
		[DataRow("\"{x:Bind }\"", "\"{x:Bind }\"")]
		[DataRow("\"{x:Bind a}\"", "\"{x:Bind YQA_}\"")]
		[DataRow("\"{x:Bind Test(a)}\"", "\"{x:Bind VABlAHMAdAAoAGEAKQA_}\"")]
		[DataRow("\"{x:Bind Test(a,b)}\"", "\"{x:Bind VABlAHMAdAAoAGEALABiACkA}\"")]
		[DataRow("\"{x:Bind Test(a,b), Converter={StaticResource Res1}}\"", "\"{x:Bind VABlAHMAdAAoAGEALABiACkA, Converter={StaticResource Res1}}\"")]
		[DataRow("\"{x:Bind Test(a, (int)b), Converter={StaticResource Res1}}\"", "\"{x:Bind VABlAHMAdAAoAGEALAAgACgAaQBuAHQAKQBiACkA, Converter={StaticResource Res1}}\"")]
		[DataRow("\"{x:Bind dateTimeField,Converter={StaticResource StringFormatConverter},ConverterParameter='Started: {0:MMM dd, yyyy hh:mm tt}',Mode=OneWay}\"", "\"{x:Bind ZABhAHQAZQBUAGkAbQBlAEYAaQBlAGwAZAA_,Converter={StaticResource StringFormatConverter},ConverterParameter='Started: {0:MMM dd, yyyy hh:mm tt}',Mode=OneWay}\"")]
		[DataRow("\"{x:Bind sys:String.Format('{0}, ^'{1}^'', InstanceProperty, StaticProperty)}\"", "\"{x:Bind cwB5AHMAOgBTAHQAcgBpAG4AZwAuAEYAbwByAG0AYQB0ACgAJwB7ADAAfQAsACAAlCGUIXsAMQB9AJQhlCEnACwAIABJAG4AcwB0AGEAbgBjAGUAUAByAG8AcABlAHIAdAB5ACwAIABTAHQAYQB0AGkAYwBQAHIAbwBwAGUAcgB0AHkAKQA_}\"")]

		// Named parameter
		[DataRow("\"{x:Bind Path=a}\"", "\"{x:Bind Path=YQA_}\"")]
		[DataRow("\"{x:Bind Path=Test(a)}\"", "\"{x:Bind Path=VABlAHMAdAAoAGEAKQA_}\"")]
		[DataRow("\"{x:Bind Converter={StaticResource Res1}, Path=a}\"", "\"{x:Bind Converter={StaticResource Res1},Path=YQA_}\"")]
		[DataRow("\"{x:Bind Converter={StaticResource Res1}, Path=Test(a)}\"", "\"{x:Bind Converter={StaticResource Res1},Path=VABlAHMAdAAoAGEAKQA_}\"")]
		[DataRow("\"{x:Bind Converter={StaticResource DebugConverter}, Mode=OneWay, ConverterParameter=test}\"", "\"{x:Bind Converter={StaticResource DebugConverter}, Mode=OneWay, ConverterParameter=test}\"")]

		public void ParseXBind(string bindSource, string expected)
		{
			var result = XBindExpressionParser.RewriteDocumentPaths(bindSource);

			Assert.AreEqual(expected, result);
		}
	}
}
