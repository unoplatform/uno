#if !__WASM__
using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Documents;

public partial class SegmentsCollection<T> : DependencyObjectCollection<T>
	where T : DependencyObject
{
	private DependencyObject[] _preorderTree;

	internal SegmentsCollection(DependencyObject parent, bool isAutoPropertyInheritanceEnabled = true)
		: base(parent, isAutoPropertyInheritanceEnabled)
	{

	}

	private protected override void OnCollectionChanged()
	{
#if !IS_UNIT_TESTS
		_preorderTree = null;
#endif
	}

	/// <summary>
	/// Gets a list of all tree elements.
	/// </summary>
	/// <returns>
	/// The list includes both nested elements and their parents.
	/// </returns>
	/// <remarks>
	/// In following example:
	/// <code>
	/// Paragraph
	///	    |_Span
	///	        |_Run
	///	        |_Run
	///	</code>
	///	The result will be: <code>{ Paragraph, Span, Run, Run }</code>
	/// </remarks>
	public DependencyObject[] PreorderTree => _preorderTree ??= GetPreorderTree();

	private DependencyObject[] GetPreorderTree()
	{
		if (this.Count == 1
			&& this[0] is not Span
			&& this[0] is not Paragraph)
		{
			return new DependencyObject[] { this[0] };
		}
		else if (this.Count == 0)
		{
			return Array.Empty<DependencyObject>();
		}
		else
		{
			var result = new List<DependencyObject>(4);

			var enumerator = this.GetEnumeratorFast();

			while (enumerator.MoveNext())
			{
				GetPreorderTreeInner(enumerator.Current, result);
			}

			return result.ToArray();
		}

		static void GetPreorderTreeInner(DependencyObject inline, List<DependencyObject> accumulator)
		{
			accumulator.Add(inline);

			if (inline is Span span)
			{
				var enumerator = span.Inlines.GetEnumeratorFast();

				while (enumerator.MoveNext())
				{
					GetPreorderTreeInner(enumerator.Current, accumulator);
				}
			}
			else if (inline is Paragraph paragraph)
			{
				var enumerator = paragraph.Inlines.GetEnumeratorFast();
				while (enumerator.MoveNext())
				{
					GetPreorderTreeInner(enumerator.Current, accumulator);
				}
			}
		}
	}
}

public partial class SegmentsCollection : SegmentsCollection<DependencyObject>
{
	internal SegmentsCollection(DependencyObject parent, bool isAutoPropertyInheritanceEnabled = true)
		: base(parent, isAutoPropertyInheritanceEnabled)
	{

	}
}
#endif
