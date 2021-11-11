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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Equality;

namespace Uno.Extensions
{
	internal static class EqualityExtensions
	{
		public static EqualityExtensionPoint<T> Equality<T>(this T instance)
		{
			return new EqualityExtensionPoint<T>(instance);
		}

		public static int GetHashCode<T>(this EqualityExtensionPoint<T> extensionPoint, IEnumerable<object> items)
		{
			var hashCode = typeof(T).GetHashCode();

			foreach (var item in items)
			{
				if (item != null)
				{
					var newHashCode = 0;
					// Will need to get the hashcode of all the items of the array 
					// and since a string is IEnumerable, we want to also exclude them from that logic
					if (item is IEnumerable &&
						!(item is string))
					{
						foreach (var hashItem in item as IEnumerable)
						{
							newHashCode ^= hashItem.GetHashCode();
						}
					}
					else
					{
						newHashCode = item.GetHashCode();
					}
					hashCode = (int)(0xa5555529 * hashCode) + newHashCode;
				}
			}

			return hashCode;
		}

		public static int GetHashCode<T>(this EqualityExtensionPoint<T> extensionPoint,
											Func<T, IEnumerable<object>> items)
		{
			return GetHashCode(extensionPoint, items(extensionPoint.ExtendedValue));
		}

		public static bool Equal<T>(this EqualityExtensionPoint<T> x, T y)
		{
			var xValue = x.ExtendedValue;
			if (xValue == null)
			{
				return y == null;
			}

			if (y == null)
			{
				return false;
			}

			if (!(xValue is IEnumerable) && xValue.GetHashCode() != y.GetHashCode())
			{
				return false;
			}

			if (Equals(x.ExtendedValue, y))
			{
				return true;
			}

			if (x.ExtendedValue is IEnumerable xItems)
			{
				if (y is IEnumerable yItems)
				{
					return Equals(xItems, yItems, Predicates<object>.Equal);
				}
			}

			return false;
		}

		public static bool Equal<T>(this EqualityExtensionPoint<T> x, object y, Func<T, IEnumerable<object>> content)
		{
			var xValue = x.ExtendedValue;
			if (xValue == null || y == null)
			{
				return false;
			}

			if (xValue.GetHashCode() != y.GetHashCode())
			{
				return false;
			}

			if (y is T variable)
			{
				return Equal(x, variable, content);
			}
			return false;
		}

		public static bool Equal<T>(this EqualityExtensionPoint<T> x, T y, Func<T, IEnumerable<object>> content)
		{
			return Equal(x, y, content, Predicates<object>.Equal);
		}

		public static bool Equal<T>(this EqualityExtensionPoint<T> x, T y, Func<T, IEnumerable<object>> content,
									IEqualityComparer comparer)
		{
			return Equal(x, y, content, comparer.Equals);
		}

		public static bool Equal<T>(this EqualityExtensionPoint<T> x, T y, Func<T, IEnumerable<object>> content,
									Func<object, object, bool> predicate)
		{
			var xValue = x.ExtendedValue;
			if (xValue == null || y == null)
			{
				return false;
			}

			if (xValue.GetHashCode() != y.GetHashCode())
			{
				return false;
			}

			var xItems = content(x.ExtendedValue);
			var yItems = content(y);

			return Equals(xItems, yItems, predicate);
		}

		//TODO Useful?
		public static bool Equal<T>(this EqualityExtensionPoint<T> xItems, IEnumerable yItems)
			where T : IEnumerable
		{
			return Equal(xItems, yItems, Predicates<object>.Equal);
		}

		public static bool Equal<T>(this EqualityExtensionPoint<T> xItems, IEnumerable yItems,
									IEqualityComparer comparer)
			where T : IEnumerable
		{
			return Equal(xItems, yItems, comparer.Equals);
		}

		public static bool Equal<T>(this EqualityExtensionPoint<T> xItems, IEnumerable yItems,
									Func<object, object, bool> predicate)
			where T : IEnumerable
		{
			return Equals(xItems.ExtendedValue, yItems, predicate);
		}

		public static bool Equal<T, TItem>(this EqualityExtensionPoint<T> xItems, IEnumerable<TItem> yItems)
			where T : IEnumerable<TItem>
		{
			return Equal(xItems, yItems, Predicates<TItem>.Equal);
		}

		public static bool Equal<T, TItem>(this EqualityExtensionPoint<T> xItems, IEnumerable<TItem> yItems,
											IEqualityComparer<TItem> comparer)
			where T : IEnumerable<TItem>
		{
			return Equal(xItems, yItems, comparer.Equals);
		}

		public static bool Equal<T, TItem>(this EqualityExtensionPoint<T> xItems, IEnumerable<TItem> yItems,
											Func<TItem, TItem, bool> predicate)
			where T : IEnumerable<TItem>
		{
			return xItems.ExtendedValue.SequenceEqual(yItems, predicate.Equality().ToComparer());
		}

		public static IEqualityComparer<T> ToComparer<T>(this EqualityExtensionPoint<Func<T, T, bool>> extensionPoint)
		{
			return new FuncEqualityComparer<T>(extensionPoint.ExtendedValue);
		}

		public static bool Equals(IEnumerable xItems, IEnumerable yItems, Func<object, object, bool> predicate)
		{
			if (object.ReferenceEquals(xItems, yItems))
			{
				return true;
			}

			var xNewItems = xItems.Cast<object>();
			var yNewItems = yItems.Cast<object>();

			return xNewItems.SequenceEqual(yNewItems, predicate.Equality().ToComparer());
		}
	}
}
