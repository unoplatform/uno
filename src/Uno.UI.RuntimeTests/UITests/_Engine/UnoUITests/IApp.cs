using System;
using System.Linq;
using Uno.UITest.Helpers.Queries;

namespace Uno.UITest;

public interface IApp
{
	void TapCoordinates(double x, double y);
	void DragCoordinates(double fromX, double fromY, double toX, double toY);

	internal QueryResult[] Query(string marked);
	internal QueryResult[] Query(IAppQuery query);
	internal QueryResult[] Query(Func<IAppQuery, IAppQuery> query);
}
