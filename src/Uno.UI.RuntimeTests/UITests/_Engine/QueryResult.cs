using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UITest.Helpers.Queries;

internal class QueryResult
{
	private readonly UIElement _element;

	public QueryResult(UIElement element)
	{
		_element = element;
	}

	public UIElement Element => _element;

	public AppRect Rect => _element.TransformToVisual(null).TransformBounds(new Rect(default, _element.RenderSize));
}
