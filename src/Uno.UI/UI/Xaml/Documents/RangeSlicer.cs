#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Xaml.Documents;

internal class RangeSlicer<TValue>
{
	public struct Segment : IEquatable<Segment>
	{
		public long Start { get; }
		public long End { get; }
		public TValue? Value { get; }
		[MemberNotNullWhen(true, nameof(Value))]
		public bool HasValue { get; }

		public Segment(long start, long end, TValue? value, bool hasValue)
		{
			Start = start;
			End = end;
			Value = value;
			HasValue = hasValue;
		}

		public override bool Equals(object? obj) => obj is Segment other && Equals(other);

		public bool Equals(Segment other)
		{
			return Start == other.Start &&
				   End == other.End &&
				   EqualityComparer<TValue?>.Default.Equals(Value, other.Value) &&
				   HasValue == other.HasValue;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Start, End, Value, HasValue);
		}

		public static bool operator ==(Segment left, Segment right) => left.Equals(right);
		public static bool operator !=(Segment left, Segment right) => !left.Equals(right);

		public override string ToString() => $"[{Start}, {End}) = {(HasValue ? Value?.ToString() : "Unset")}";
	}

	private List<Segment> _segments = new();
	private readonly long _initialStart;
	private readonly long _initialEnd;

	public RangeSlicer(long start, long end)
	{
		if (start > end) throw new ArgumentOutOfRangeException(nameof(start), "Start must be less than or equal to End.");
		_initialStart = start;
		_initialEnd = end;
		_segments.Add(new Segment(start, end, default, false));
	}

	/// <summary>
	/// Mark a subsequence with a value. The range must be within the initial range.
	/// </summary>
	public void Mark(long start, long end, TValue value)
	{
		if (start > end) throw new ArgumentOutOfRangeException(nameof(start), "Start must be less than or equal to End.");

		if (start == end)
		{
			return;
		}

		if (end > _initialEnd)
		{
			end = _initialEnd;
		}

		if (start < _initialStart || start > end)
		{
			throw new ArgumentOutOfRangeException(nameof(start), "Range is outside the initial bounds.");
		}

		var newSegments = new List<Segment>(_segments.Count + 2);
		bool inserted = false;

		foreach (var existing in _segments)
		{
			// Case 1: Existing is completely before new range
			if (existing.End <= start)
			{
				newSegments.Add(existing);
				continue;
			}

			// Case 2: Existing is completely after new range
			if (existing.Start >= end)
			{
				if (!inserted)
				{
					newSegments.Add(new Segment(start, end, value, true));
					inserted = true;
				}
				newSegments.Add(existing);
				continue;
			}

			// Case 3: Overlap

			// Left part of existing?
			if (existing.Start < start)
			{
				newSegments.Add(new Segment(existing.Start, start, existing.Value, existing.HasValue));
			}

			// Insert new segment if not already inserted
			// This happens at the first overlap encounter
			if (!inserted)
			{
				newSegments.Add(new Segment(start, end, value, true));
				inserted = true;
			}

			// Right part of existing?
			if (existing.End > end)
			{
				newSegments.Add(new Segment(end, existing.End, existing.Value, existing.HasValue));
			}
		}

		// This case should theoretically not happen if the range is within bounds and _segments covers the full range
		if (!inserted)
		{
			newSegments.Add(new Segment(start, end, value, true));
		}

		_segments = MergeSegments(newSegments);
	}

	private List<Segment> MergeSegments(List<Segment> segments)
	{
		if (segments.Count == 0) return segments;

		var merged = new List<Segment>(segments.Count);
		var current = segments[0];

		for (int i = 1; i < segments.Count; i++)
		{
			var next = segments[i];

			// If adjacent and same value/state, merge
			if (current.End == next.Start &&
				current.HasValue == next.HasValue &&
				EqualityComparer<TValue?>.Default.Equals(current.Value, next.Value))
			{
				current = new Segment(current.Start, next.End, current.Value, current.HasValue);
			}
			else
			{
				merged.Add(current);
				current = next;
			}
		}
		merged.Add(current);
		return merged;
	}

	public List<Segment> GetSegments()
	{
		return _segments;
	}
}
