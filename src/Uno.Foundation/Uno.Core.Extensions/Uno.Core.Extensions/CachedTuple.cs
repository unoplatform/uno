// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;
using System.Text;

#if IS_UNO_FOUNDATION_RUNTIME_WEBASSEMBLY_PROJECT
namespace Uno.Foundation.Runtime.WebAssembly.Helpers
#else
namespace Uno
#endif
{
	/// <summary>
	/// A tuple implementation that caches the GetHashCode value for faster lookup performance.
	/// </summary>
	internal class CachedTuple
    {
		/// <summary>
		/// Creates a tuple with two values.
		/// </summary>
		public static CachedTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
		{
			return new CachedTuple<T1, T2>(item1, item2);
		}

		/// <summary>
		/// Creates a tuple with three values.
		/// </summary>
		public static CachedTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
		{
			return new CachedTuple<T1, T2, T3>(item1, item2, item3);
		}

		/// <summary>
		/// Creates a tuple with four values.
		/// </summary>
		public static CachedTuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
		{
			return new CachedTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
		}
	}

	/// <summary>
	/// A tuple with two values implementation that caches the GetHashCode value for faster lookup performance.
	/// </summary>
	internal class CachedTuple<T1, T2>
	{
		private readonly int _cachedHashCode;

		public T1 Item1 { get; }
		public T2 Item2 { get; }

		/// <summary>
		/// Gets a comparer for the current tuple
		/// </summary>
		public static readonly IEqualityComparer<CachedTuple<T1, T2>> Comparer = new EqualityComparer();

		public CachedTuple(T1 item1, T2 item2)
		{
			Item1 = item1;
			Item2 = item2;

			_cachedHashCode = item1?.GetHashCode() ?? 0
				^ item2?.GetHashCode() ?? 0;
		}

		public override int GetHashCode() => _cachedHashCode;

		public override bool Equals(object obj)
		{
			var tuple = obj as CachedTuple<T1, T2>;

			if (tuple != null)
			{
				return InternalEquals(this, tuple);
			}

			return false;
		}

		private static bool InternalEquals(CachedTuple<T1, T2> t1, CachedTuple<T1, T2> t2)
		{
			return ReferenceEquals(t1, t2) || (
				Equals(t1.Item1, t2.Item1)
				&& Equals(t1.Item2, t2.Item2)
			);
		}

		private class EqualityComparer : IEqualityComparer<CachedTuple<T1, T2>>
		{
			public bool Equals(CachedTuple<T1, T2> x, CachedTuple<T1, T2> y)
			{
				return InternalEquals(x, y);
			}

			public int GetHashCode(CachedTuple<T1, T2> obj)
			{
				return obj._cachedHashCode;
			}
		}
	}

	/// <summary>
	/// A tuple with three values implementation that caches the GetHashCode value for faster lookup performance.
	/// </summary>
	internal class CachedTuple<T1, T2, T3>
	{
		private readonly int _cachedHashCode;

		/// <summary>
		/// Gets a comparer for the current tuple
		/// </summary>
		public static readonly IEqualityComparer<CachedTuple<T1, T2, T3>> Comparer = new EqualityComparer();

		public T1 Item1 { get; }
		public T2 Item2 { get; }
		public T3 Item3 { get; }

		public CachedTuple(T1 item1, T2 item2, T3 item3)
		{
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;

			_cachedHashCode = 
				item1?.GetHashCode() ?? 0 
				^ item2?.GetHashCode() ?? 0
				^ item3?.GetHashCode() ?? 0;
		}

		public override int GetHashCode() => _cachedHashCode;

		public override bool Equals(object obj)
		{
			var tuple = obj as CachedTuple<T1, T2, T3>;

			if (tuple != null)
			{
				return InternalEquals(this, tuple);
			}

			return false;
		}

		private static bool InternalEquals(CachedTuple<T1, T2, T3> t1, CachedTuple<T1, T2, T3> t2)
		{
			return ReferenceEquals(t1, t2) || (
				Equals(t1.Item1, t2.Item1)
				&& Equals(t1.Item2, t2.Item2)
				&& Equals(t1.Item3, t2.Item3)
			);
		}

		private class EqualityComparer : IEqualityComparer<CachedTuple<T1, T2, T3>>
		{
			public bool Equals(CachedTuple<T1, T2, T3> x, CachedTuple<T1, T2, T3> y)
			{
				return InternalEquals(x, y);
			}

			public int GetHashCode(CachedTuple<T1, T2, T3> obj)
			{
				return obj._cachedHashCode;
			}
		}
	}

	/// <summary>
	/// A tuple with four values implementation that caches the GetHashCode value for faster lookup performance.
	/// </summary>
	internal class CachedTuple<T1, T2, T3, T4>
	{
		private readonly int _cachedHashCode;

		public static readonly IEqualityComparer<CachedTuple<T1, T2, T3, T4>> Comparer = new EqualityComparer();

		public T1 Item1 { get; }
		public T2 Item2 { get; }
		public T3 Item3 { get; }
		public T4 Item4 { get; }

		public CachedTuple(T1 item1, T2 item2, T3 item3, T4 item4)
		{
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
			Item4 = item4;

			_cachedHashCode = item1?.GetHashCode() ?? 0 
				^ item2?.GetHashCode() ?? 0
				^ item3?.GetHashCode() ?? 0
				^ item4?.GetHashCode() ?? 0;
		}

		public override int GetHashCode() => _cachedHashCode;

		public override bool Equals(object obj)
		{
			var tuple = obj as CachedTuple<T1, T2, T3, T4>;

			if (tuple != null)
			{
				return InternalEquals(this, tuple);
			}

			return false;
		}

		private static bool InternalEquals(CachedTuple<T1, T2, T3, T4> t1, CachedTuple<T1, T2, T3, T4> t2)
		{
			return ReferenceEquals(t1, t2) || (
					Equals(t1.Item1, t2.Item1)
					&& Equals(t1.Item2, t2.Item2)
					&& Equals(t1.Item3, t2.Item3)
					&& Equals(t1.Item4, t2.Item4)
				);
		}

		private class EqualityComparer : IEqualityComparer<CachedTuple<T1, T2, T3, T4>>
		{
			public bool Equals(CachedTuple<T1, T2, T3, T4> x, CachedTuple<T1, T2, T3, T4> y)
			{
				return InternalEquals(x, y);
			}

			public int GetHashCode(CachedTuple<T1, T2, T3, T4> obj)
			{
				return obj._cachedHashCode;
			}
		}
	}
}
