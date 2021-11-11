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
using Uno.Extensions;
using Uno.Extensions.Specialized;

namespace Uno
{
    /// <summary>
    /// Represents a Key concept.
    /// </summary>
    internal class Key
    {
        private static readonly Func<Key, IEnumerable<object>> Fields = item => item.Items;

        /// <summary>
        /// Constucts a new Key with an array of items.
        /// </summary>
        /// <param name="items"></param>
        public Key(params object[] items)
        {
            Items = items;
        }

        /// <summary>
        /// Constucts a new Key with an enumeration of items.
        /// </summary>
        /// <param name="items"></param>
        public Key(IEnumerable items)
            : this(items.ToObjectArray())
        {
        }

        public object[] Items { get; private set; }

        /// <summary>
        /// See Object pattern.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Equality().GetHashCode(Fields);
        }

        /// <summary>
        /// See Object pattern.
        /// </summary>
        public override bool Equals(object obj)
        {
            return this.Equality().Equal(obj, Fields);
        }
    }
}