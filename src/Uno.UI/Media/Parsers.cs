using Windows.UI.Xaml.Media;
using System;

namespace Uno.Media
{
	static class Parsers
	{
		internal static Geometry ParseGeometry(string pathString, IFormatProvider formatProvider)
		{
			FillRule fillRule = FillRule.EvenOdd;
			var streamGeometry = new StreamGeometry();
			using (StreamGeometryContext context = streamGeometry.Open())
			{
				var parser = new PathMarkupParser(context);
				parser.Parse(pathString, ref fillRule);
			}
			streamGeometry.FillRule = fillRule;
			return streamGeometry;
		}
	}
}

