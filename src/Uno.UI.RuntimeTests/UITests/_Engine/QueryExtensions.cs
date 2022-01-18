using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.Foundation;
using Windows.UI.Xaml;
using Uno.UITest;

namespace Uno.UITest.Helpers.Queries;

internal static class QueryExtensions
{
	public static IEnumerable<QueryResult> WaitForElement(this SkiaApp app, string elementName)
		=> QueryEx.Any(app).Marked(elementName).Execute();

	public static QueryEx Marked(this SkiaApp app, string elementName)
		=> QueryEx.Any(app).Marked(elementName);

	public static void Tap(this QueryEx query)
	{
		var bounds = query.Execute().Single().Rect;

		query.App.TapCoordinates((float)(bounds.X + bounds.Width / 2), (float)(bounds.Y + bounds.Height / 2));
	}

	public static void Tap(this SkiaApp app, string elementName)
		=> app.Marked(elementName).Tap();

	public static void FastTap(this QueryEx query)
		=> query.Tap();

	public static void FastTap(this SkiaApp app, string elementName)
		=> app.Marked(elementName).Tap();

	public static object GetDependencyPropertyValue(this QueryEx query, string propertyName)
	{
		var elt = query.Execute().Single().Element;
		var property = elt.GetType().GetProperty(propertyName + "Property", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(elt) as DependencyProperty;
		if (property is null)
		{
			throw new InvalidOperationException($"Cannot get property named '{propertyName}'.");
		}

		return elt.GetValue(property);
	}
}
