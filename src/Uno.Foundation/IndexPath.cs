using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Uno.UI;

/// <summary>
/// An index to an entry in a grouped items source.
/// </summary>
[DebuggerDisplay("{DebugDisplay,nq}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public partial struct IndexPath : IComparable<IndexPath>
{
	public int Row { get; }
	public int Section { get; }

	private IndexPath(int row, int section)
	{
		Row = row;
		Section = section;
	}

	public static IndexPath FromRowSection(int row, int section)
	{
		return new IndexPath(row, section);
	}

	int IComparable<IndexPath>.CompareTo(IndexPath other)
	{
		return Compare(this, other);
	}

	public static IndexPath Zero { get; }

	public static IndexPath NotFound { get; } = new IndexPath(-1, 0);

	public static bool operator <(IndexPath indexPath1, IndexPath indexPath2)
	{
		return Compare(indexPath1, indexPath2) < 0;
	}

	public static bool operator >(IndexPath indexPath1, IndexPath indexPath2)
	{
		return Compare(indexPath1, indexPath2) > 0;
	}

	public static bool operator <=(IndexPath indexPath1, IndexPath indexPath2)
	{
		return Compare(indexPath1, indexPath2) <= 0;
	}

	public static bool operator >=(IndexPath indexPath1, IndexPath indexPath2)
	{
		return Compare(indexPath1, indexPath2) >= 0;
	}

	public static bool operator ==(IndexPath indexPath1, IndexPath indexPath2)
	{
		return Compare(indexPath1, indexPath2) == 0;
	}

	public static bool operator !=(IndexPath indexPath1, IndexPath indexPath2)
	{
		return !(indexPath1 == indexPath2);
	}

	private static int Compare(IndexPath thisOne, IndexPath other)
	{
		if (thisOne.Section < other.Section)
		{
			return -1;
		}
		if (thisOne.Section > other.Section)
		{
			return 1;
		}

		//Sections are equal
		if (thisOne.Row < other.Row)
		{
			return -1;
		}
		if (thisOne.Row > other.Row)
		{
			return 1;
		}

		return 0;
	}

	public override string ToString()
	{
		return $"({Section},{Row})";
	}

	private string DebugDisplay => ToString();

	public override bool Equals(object obj)
	{
		if (!(obj is IndexPath))
		{
			return false;
		}

		return (IndexPath)obj == this;
	}

	public override int GetHashCode()
	{
		var hash = 13;
		hash = hash * 7 + Row.GetHashCode();
		hash = hash * 7 + Section.GetHashCode();
		return hash;
	}
}
