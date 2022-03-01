using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Uno.UITest;

namespace Uno.UITest.Helpers.Queries;

internal static class Helpers
{
	internal static SkiaApp App { get; set; }
}

internal static partial class QueryExtensions
{
	// This class contains extensions method to match the Uno.UITest.Helpers.Queries.QueryExtensions contract.

	private static SkiaApp App => Helpers.App;

	public static void Tap(this QueryEx query)
	{
		var bounds = App.Query(query).Single().Rect;

		App.TapCoordinates((float)(bounds.X + bounds.Width / 2), (float)(bounds.Y + bounds.Height / 2));
	}

	public static object GetDependencyPropertyValue(this QueryEx query, string propertyName)
	{
		var elt = App.Query(query).Single().Element;
		var property = elt.GetType().GetProperty(propertyName + "Property", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(elt) as DependencyProperty;
		if (property is null)
		{
			throw new InvalidOperationException($"Cannot get property named '{propertyName}'.");
		}

		return elt.GetValue(property);
	}

	public static T GetDependencyPropertyValue<T>(this QueryEx query, string propertyName)
		=> (T)query.GetDependencyPropertyValue(propertyName);
}
