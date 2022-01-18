using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Private.Infrastructure;
using Uno.UI.Extensions;

namespace Uno.UITest.Helpers.Queries;

internal class QueryEx
{
	private readonly Func<IEnumerable<QueryResult>> _query;

	public static QueryEx Any(SkiaApp app)
		=> new(
			app,
			() => TestServices.WindowHelper.WindowContent
				.GetAllChildren(includeCurrent: true)
				.OfType<FrameworkElement>()
				.Select(elt => new QueryResult(elt)));

	private QueryEx(SkiaApp app, Func<IEnumerable<QueryResult>> query)
	{
		App = app;
		_query = query;
	}

	public SkiaApp App { get; }

	public QueryEx Marked(string marked)
		=> new (App, () => _query().Where(result => result.Element.Name == marked));

	public QueryEx AtIndex(int index)
		=> new(App, () => _query().Skip(index).Take(1));

	internal IEnumerable<QueryResult> Execute()
		=> _query();
}
