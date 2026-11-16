using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.Media.Core;

using _View = Microsoft.UI.Xaml.DependencyObject;

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
