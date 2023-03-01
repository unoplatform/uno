using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils;

/// <summary>
/// A SourceText implementation that is more performant for cases when we already have a StringBuilder.
/// In Xaml generator, sometimes our output code gets very large and calling builder.ToString() allocates in Large Object Heap (LOH).
/// LOH allocations are costly and we want to avoid them, so we implement our own SourceText instead of using `SourceText.From(builder.ToString())`
/// </summary>
/// <remarks>
/// Once this SourceText is created, the builder should NOT be altered in anyway.
/// </remarks>
internal sealed class StringBuilderBasedSourceText : SourceText
{
	private readonly StringBuilder _builder;

	public StringBuilderBasedSourceText(StringBuilder builder)
	{
		_builder = builder;
	}

	public override char this[int position] => _builder[position];

	public override Encoding Encoding => Encoding.UTF8;

	public override int Length => _builder.Length;

	public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
		=> _builder.CopyTo(sourceIndex, destination, destinationIndex, count);

	public override string ToString(TextSpan span)
		=> _builder.ToString(span.Start, span.Length);
}
