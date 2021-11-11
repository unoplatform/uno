#nullable enable

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
using System.Linq;
using System;
using System.Collections.Generic;

namespace Uno.Equality
{
	internal class WeakReferenceEqualityComparer<T> : IEqualityComparer<System.WeakReference<T>>
		where T : class
	{
		public bool Equals(System.WeakReference<T> w1, T t2)
		{
			T? t1;

			var r1 = w1.TryGetTarget(out t1);

			return (!r1 && t1 != null)
				|| SafeEquals(t1, t2);
		}


		public bool Equals(System.WeakReference<T>? w1, System.WeakReference<T>? w2)
		{
			T? t1 = null;
			T? t2 = null;

			var r1 = w1?.TryGetTarget(out t1) ?? false;
			var r2 = w2?.TryGetTarget(out t2) ?? false;

			return ((r1 && r2) || (!r1 && !r2))
				&& SafeEquals<T>(t1, t2);
		}

		public int GetHashCode(System.WeakReference<T> w)
		{
			T? t;

			return w.TryGetTarget(out t)
				? t.GetHashCode()
				: -1;
		}

		private static bool SafeEquals<T2>(T2? obj, T2? other)
			where T2 : class
		{
			if (obj == null)
			{
				return other == null;
			}
			else
			{
				return obj.Equals(other);
			}
		}
	}
}
