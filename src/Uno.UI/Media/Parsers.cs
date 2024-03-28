using Windows.UI.Xaml.Media;

namespace Uno.Media
{
	static class Parsers
	{
		internal static Geometry ParseGeometry(string pathString)
		{
			var fillRule = FillRule.EvenOdd;
			var streamGeometry = new StreamGeometry();
			using (var context = streamGeometry.Open())
			{
				var parser = new PathMarkupParser(context);
				parser.Parse(pathString, ref fillRule);
			}
			streamGeometry.FillRule = fillRule;
			return streamGeometry;
		}
	}
}

