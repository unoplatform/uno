using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.UITest.Helpers.Queries;

internal static class RuntimeTestsAppExtensions
{
	// This class contains compatibility method to match the Uno.UITest.IApp contract.
	// Those methods are directly on the IApp interface but are not expected to be imported on the refreshed
	// IAppQuery interface of runtime tests (where we try to keep only the core versions like 'Query' and 'xxxxCoordinates').

	public static QueryEx Marked(this IApp app, string elementName)
		=> new(q => q.Marked(elementName));

	public static void Tap(this IApp app, string elementName)
		=> app.Marked(elementName).Tap();

	// WARNING: Those version does not wait on runtime UI tests ...
	// TODO: We need to create async versions (and remove those!).
	public static IEnumerable<QueryResult> WaitForElement(this IApp app, string elementName)
		=> app.Query(q => q.Marked(elementName));

	public static IEnumerable<QueryResult> WaitForElement(this IApp app, QueryEx query)
		=> app.Query(query);

	public static IEnumerable<QueryResult> WaitForElement(this IApp app, Func<IAppQuery, IAppQuery> query)
		=> app.Query(query);


}
