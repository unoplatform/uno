using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.Media.Core;

#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#elif __ANDROID__
using _View = Android.Views.View;
#else
using _View = Microsoft.UI.Xaml.DependencyObject;
#endif

namespace Uno.UI.RuntimeTests.Helpers;

internal static class TreeAssert
{
	private record NodeInfo(int Line, int Depth, string Name, string Description);

	/// <summary>
	/// Verify every node in a tree matches the description as in the expected values
	/// </summary>
	/// <param name="expectedTree"></param>
	/// <param name="root"></param>
	/// <param name="flatten"></param>
	/// <param name="describe"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public static void VerifyTree(string expectedTree, object root, Func<object, IEnumerable<(int Depth, object Node)>> flatten = null, Func<object, IEnumerable<string>> describe = null)
	{
		if (root is null) throw new ArgumentNullException(nameof(root));

		var expectations = expectedTree
			.Split('\n', StringSplitOptions.TrimEntries)
			.Select((x, i) =>
			{
				var line = x.TrimStart("0123456789. ".ToArray());
				var depth = line.TakeWhile(x => x == '\t').Count() - 1;
				var parts = line.Split("//", 2, StringSplitOptions.TrimEntries);

				return new NodeInfo(i, depth, parts[0], parts.ElementAtOrDefault(1) ?? string.Empty);
			})
#if __ANDROID__ || __IOS__
			// On droid and ios, ContentPresenter bypass can be potentially enabled (based on if a base control template is present, or not).
			// As such, ContentPresenter may be omitted, and altering its visual descendants.
			.Aggregate(
				new { DroppedDepths = new Stack<int>(), Results = new List<NodeInfo>() },
				(acc, x) => // drop ignored line, and repair depth from dropped item
				{
					if (x.Description.Contains("IGNORE_FOR_MOBILE_CP_BYPASS"))
					{
						acc.DroppedDepths.Push(x.Depth);
						return acc;
					}

					if (acc.DroppedDepths.TryPeek(out var dropped))
					{
						if (dropped >= x.Depth) acc.DroppedDepths.Pop();
						acc.Results.Add(x with { Depth = x.Depth - acc.DroppedDepths.Count(y => y < x.Depth) });
					}
					else
					{
						acc.Results.Add(x);
					}

					return acc;
				},
				acc => acc.Results
			)
#endif
			.ToList();
		var descendants = (flatten?.Invoke(root) ?? FlattenVT(root)).ToArray();

		Assert.HasCount(expectations.Count, descendants, "Mismatched descendant size");
		for (int i = 0; i < expectations.Count; i++)
		{
			var expected = expectations[i];

			var node = descendants[i];
			var name = PrettyPrint.FormatType(node.Node);

			Assert.AreEqual(expected.Depth, node.Depth, $"Incorrect depth on line {expected.Line}");
			Assert.AreEqual(expected.Name, name, $"Incorrect node on line {expected.Line}");
			if (!expected.Description.Contains("SKIP_DESC_COMPARE"))
			{
				var description = string.Join(", ", describe?.Invoke(node.Node) ?? Array.Empty<string>());
				Assert.AreEqual(expected.Description, description, $"Invalid description on line {expected.Line}");
			}
		}
	}

	private static IEnumerable<(int Depth, object Node)> FlattenVT(object node, int depth = 0)
	{
		yield return (depth, node);

		var children = (node as _View)?.EnumerateChildren().Cast<object>();
		if (children is { })
		{
			foreach (var child in children)
			{
				foreach (var nested in FlattenVT(child, depth + 1))
				{
					yield return nested;
				}
			}
		}
	}
}
