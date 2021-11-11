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
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions
{
    /// <summary>
    /// Provides Extensions Methods for IGrouping.
    /// </summary>
    internal static class GroupingExtensions
    {
        /// <summary>
        /// Adapts a IEnumarable of a IGrouping into a IDictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the Key.</typeparam>
        /// <typeparam name="TElement">The type of the grouped element.</typeparam>
        /// <param name="items">The groupings to adapt.</param>
        /// <returns>A Dictionary containing the contents of the grouping.</returns>
        public static Dictionary<TKey, IEnumerable<TElement>>
            ToGroupedDictionary<TKey, TElement>(this IEnumerable<IGrouping<TKey, TElement>> items)
        {
            return items.ToDictionary<IGrouping<TKey, TElement>, TKey, IEnumerable<TElement>>(
                item => item.Key,
                item => item);
        }
    }
}