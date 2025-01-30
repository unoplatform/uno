// ******************************************************************
// Copyright � 2015-2018 Uno Platform Inc. All rights reserved.
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
#if !SILVERLIGHT
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Extensions
{
	internal static partial class FuncExtensions
	{
		private static ConditionalWeakTable<object, ConcurrentDictionary<object, object>> _weakResults =
			new ConditionalWeakTable<object, ConcurrentDictionary<object, object>>();

		/// <summary>
		/// Creates a func that invokes the specified selector on the specified source. The result of the computation will be memoized for the specified source.
		/// </summary>
		/// <typeparam name="TSource">The type of the parameter</typeparam>
		/// <typeparam name="TResult">The type of the computation result</typeparam>
		/// <param name="source">The source instance.</param>
		/// <param name="selector">The method group to use.</param>
		/// <returns>A function that will return the memoized result</returns>
		public static Func<TResult> AsWeakMemoized<TSource, TResult>(this Func<TSource, TResult> selector, TSource source)
		{
			return () =>
			{
				var values = _weakResults.GetOrCreateValue(source);

				object res;

				bool cached = values.TryGetValue(selector, out res);

				if (!cached)
				{
					res = selector(source);

					values[selector] = res;
				}

				return (TResult)res;
			};
		}
	}
}
#endif
