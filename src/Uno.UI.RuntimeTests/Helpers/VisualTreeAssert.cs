using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Helpers;

internal interface IVisualNodePredicate : IReadOnlyList<IVisualNodePredicate>
{
	void Validate(object node);
}

internal abstract class VisualNodeAssert<T> : List<IVisualNodePredicate>, IVisualNodePredicate
{
	private readonly string _xname;

	public VisualNodeAssert(string xname, params IVisualNodePredicate[] children) : base(children)
	{
		_xname = xname;
	}

	public void Validate(object node)
	{
		Assert.IsInstanceOfType(node, typeof(T), $"Unexpected visual node");
		if (node is FrameworkElement fe)
		{
			Assert.AreEqual(fe.Name ?? string.Empty, _xname ?? string.Empty, $"Incorrect visual node xname");
		}
		ValidateCore((T)node);
	}
	protected abstract void ValidateCore(T node);
}

internal static class VisualTreeAssert
{
	public static void ValidateVisualSubtree(this DependencyObject setup, IVisualNodePredicate predicate)
	{
		predicate.Validate(setup);

		var count = VisualTreeHelper.GetChildrenCount(setup);
		Assert.AreEqual(predicate.Count, count, $"Mismatch in visual node children count");

		for (int i = 0; i < count; i++)
		{
			ValidateVisualSubtree(VisualTreeHelper.GetChild(setup, i), predicate[i]);
		}
	}
}
