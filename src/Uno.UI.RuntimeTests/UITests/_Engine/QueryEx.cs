using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Private.Infrastructure;
using Uno.UI.Extensions;

namespace Uno.UITest.Helpers.Queries;

public class QueryEx : IAppQuery
{
	private readonly Func<IEnumerable<QueryResult>, IEnumerable<QueryResult>> _query;

	public static QueryEx Any
		=> new((Func<IEnumerable<QueryResult>, IEnumerable<QueryResult>>)(elts => elts));

	public QueryEx(Func<QueryEx, QueryEx> query)
		=> _query = elts => query(Any)._query(elts);

	public QueryEx(Func<IAppQuery, IAppQuery> query)
		=> _query = elts => ((QueryEx)query(Any))._query(elts);


	private QueryEx(Func<IEnumerable<QueryResult>, IEnumerable<QueryResult>> query)
		=> _query = query;

	public QueryEx All()
		=> this;

	public QueryEx Marked(string marked)
		=> new(elts => _query(elts).Where(result => result.Element.Name == marked));

	public QueryEx AtIndex(int index)
		=> new(elts => _query(elts).Skip(index).Take(1));

	public QueryEx Descendant()
		=> new(elts => _query(elts)
			.SelectMany(result => result.Element.GetAllChildren(includeCurrent: false))
			.OfType<FrameworkElement>()
			.Select(elt => new QueryResult(elt)));

	IEnumerable<QueryResult> IAppQuery.Execute(IEnumerable<QueryResult> elements) => Execute(elements);
	internal IEnumerable<QueryResult> Execute(IEnumerable<QueryResult> elements)
		=> _query(elements);

	public static implicit operator QueryEx(string marked)
		=> Any.Marked(marked);
}
