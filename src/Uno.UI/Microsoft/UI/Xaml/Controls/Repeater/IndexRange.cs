//MUX reference IndexRange.cpp, commit de78834

using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls
{
	// Struct in WinUI
	internal partial class IndexRange
	{
		// Invariant: m_end >= m_begin
		private int m_begin = -1;
		private int m_end = -1;

		internal IndexRange()
		{
		}

		internal IndexRange(int begin, int end)
		{
			// Accept out of order begin/end pairs, just swap them.
			if (begin > end)
			{
				int temp = begin;
				begin = end;
				end = temp;
			}

			MUX_ASSERT(begin <= end);

			m_begin = begin;
			m_end = end;
		}

		internal int Begin => m_begin;

		internal int End => m_end;

		internal bool Contains(int index)
		{
			return index >= m_begin && index <= m_end;
		}

		//TODO: Scan source code for potential accidental missing ref/out! (removed &)
		internal bool Split(int splitIndex, ref IndexRange before, ref IndexRange after)
		{
			MUX_ASSERT(Contains(splitIndex));

			bool afterIsValid;

			before = new IndexRange(m_begin, splitIndex);
			if (splitIndex < m_end)
			{
				after = new IndexRange(splitIndex + 1, m_end);
				afterIsValid = true;
			}
			else
			{
				after = new IndexRange();
				afterIsValid = false;
			}

			return afterIsValid;
		}

		internal bool Intersects(IndexRange other)
		{
			return (m_begin <= other.End) && (m_end >= other.Begin);
		}

		public static bool operator ==(IndexRange lhs, IndexRange rhs)
		{
			return lhs.m_begin == rhs.m_begin && lhs.m_end == rhs.m_end;
		}

		public static bool operator !=(IndexRange lhs, IndexRange rhs) =>
			!(lhs == rhs);

		public override bool Equals(object obj)
		{
			if (obj is IndexRange rhs)
			{
				return this == rhs;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return m_begin.GetHashCode() + 13 * m_end.GetHashCode();
		}
	}
}
