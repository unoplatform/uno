using Windows.UI.Xaml.Media;
using System;
using Uno.Media;

namespace Uno.MsBuildTasks.Utils.XamlPathParser
{
	static class Parsers
	{
		internal static string ParseGeometry(string pathString, IFormatProvider formatProvider)
		{
			var fillRule = FillRule.EvenOdd;
			var context = new GeneratedStreamGeometryContext();
			var parser = new PathMarkupParser(context);
			parser.Parse(pathString, ref fillRule);
			context.FillRule = fillRule;
			return context.Generate();
		}
	}
}
