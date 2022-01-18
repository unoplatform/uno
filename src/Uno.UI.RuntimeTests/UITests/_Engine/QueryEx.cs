using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Private.Infrastructure;
using Uno.UI.Extensions;

namespace Uno.UITest.Helpers.Queries;

internal class QueryEx
{
	private readonly Func<IEnumerable<QueryResult>, IEnumerable<QueryResult>> _query;

	public static QueryEx Any
		=> new((Func<IEnumerable<QueryResult>, IEnumerable<QueryResult>>)(elts => elts));

	public QueryEx(Func<QueryEx, QueryEx> query)
		=> _query = elts => query(Any)._query(elts);

	private QueryEx(Func<IEnumerable<QueryResult>, IEnumerable<QueryResult>> query)
		=> _query = query;

	public QueryEx Marked(string marked)
		=> new (elts => _query(elts).Where(result => result.Element.Name == marked));

	public QueryEx AtIndex(int index)
		=> new(elts => _query(elts).Skip(index).Take(1));

	public QueryEx Descendant()
		=> new(elts => _query(elts)
			.SelectMany(result => result.Element.GetAllChildren(includeCurrent: false))
			.OfType<FrameworkElement>()
			.Select(elt => new QueryResult(elt)));

	internal IEnumerable<QueryResult> Execute(IEnumerable<QueryResult> elements)
		=> _query(elements);
}
