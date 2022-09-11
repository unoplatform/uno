#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.UITest.Helpers.Queries;

namespace Uno.UITest;

public interface IAppQuery
{
	QueryEx All();
	QueryEx Marked(string marked);

	internal IEnumerable<QueryResult> Execute(IEnumerable<QueryResult> elements);
}
