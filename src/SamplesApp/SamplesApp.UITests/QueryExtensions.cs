using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	public static class QueryExtensions
	{
		/// <summary>
		/// Wait for element to have the expected value for its Text property.
		/// </summary>
		public static void WaitForText(this IApp app, QueryEx element, string expectedText) =>
			app.WaitForDependencyPropertyValue(element, "Text", expectedText);

		/// <summary>
		/// Wait for element to be available and to have the expected value for its Text property.
		/// </summary>
		public static void WaitForText(this IApp app, string elementName, string expectedText, TimeSpan? timeout = null)
		{
			var element = app.Marked(elementName);
			app.WaitForElement(element, timeout: timeout);
			app.WaitForText(element, expectedText);
		}

		/// <summary>
		/// Get the value of the Text property for an element.
		/// </summary>
		public static string GetText(this QueryEx element) => element.GetDependencyPropertyValue<string>("Text");

		/// <summary>
		/// Get the value of the Text property for an element, once it's available.
		/// </summary>
		public static string GetText(this IApp app, string elementName)
		{
			var element = app.Marked(elementName);
			app.WaitForElement(element);
			return element.GetText();
		}

		/// <summary>
		/// Get bounds rect for an element.
		/// </summary>
		public static IAppRect GetRect(this IApp app, string elementName)
		{
			return app.WaitForElement(elementName).Single().Rect;
		}
		public static IAppRect GetRect(this IApp app, QueryEx query)
		{
			return app.WaitForElement(query).Single().Rect;
		}
		public static IAppRect GetRect(this IApp app, Func<IAppQuery, IAppQuery> query)
		{
			return app.WaitForElement(query).Single().Rect;
		}

		public static void FastTap(this IApp app, string elementName)
		{
			var tapPosition = app.GetRect(elementName);
			app.TapCoordinates(tapPosition.CenterX, tapPosition.CenterY);
		}

		public static void FastTap(this IApp app, QueryEx query)
		{
			var tapPosition = app.GetRect(query);
			app.TapCoordinates(tapPosition.CenterX, tapPosition.CenterY);
		}

		public static void FastTap(this IApp app, Func<IAppQuery, IAppQuery> query)
		{
			var tapPosition = app.GetRect(query);
			app.TapCoordinates(tapPosition.CenterX, tapPosition.CenterY);
		}

		public static QueryEx FastTap(this QueryEx query)
		{
			Helpers.App.FastTap(query);
			return query;
		}

		/// <summary>
		/// Wait for the named element to have received focus (ie after being tapped).
		/// </summary>
		public static void WaitForFocus(this IApp app, string elementName)
		{
			var element = app.Marked(elementName);
			app.WaitForElement(element);
			app.WaitForDependencyPropertyValue(element, "FocusState", "Pointer");
		}
	}
}
