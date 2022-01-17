using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Private.Infrastructure;
using Uno.UI.Extensions;

namespace Uno.UITest.Helpers.Queries;

internal class QueryEx
{
	private readonly string _marked;

	public QueryEx(SkiaApp app, string marked)
	{
		App = app;
		_marked = marked;
	}

	public SkiaApp App { get; }

	internal IEnumerable<QueryResult> Execute()
		=> TestServices.WindowHelper.WindowContent
			.GetAllChildren(includeCurrent: true)
			.OfType<UIElement>()
			.Where(elt => elt.Name == _marked)
			.Select(elt => new QueryResult(elt));
}
