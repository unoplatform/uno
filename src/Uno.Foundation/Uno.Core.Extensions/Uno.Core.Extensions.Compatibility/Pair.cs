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
using Uno.Extensions;

namespace Uno
{

    //TODO Replace with Tuple
    /// <summary>
    /// Represents a Pair of Ts.
    /// </summary>
    /// <typeparam name="T">The type of elements in this Pair.</typeparam>
    internal class Pair<T>
    {
        private static readonly Func<Pair<T>, IEnumerable<object>> Fields = item => new object[] {item.X, item.Y};

        /// <summary>
        /// Constructs a new Pair.
        /// </summary>
        public Pair()
        {
        }

        /// <summary>
        /// Constructs a new Pair with it's X and Y items set.
        /// </summary>
        /// <param name="x">The X item.</param>
        /// <param name="y">The Y item.</param>
        public Pair(T x, T y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Accessor for the X item.
        /// </summary>
        public T X { get; set; }

        /// <summary>
        /// Acessor for the Y item.
        /// </summary>
        public T Y { get; set; }

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

    internal class Pair<TKey, TValue>
    {
        private static readonly Func<Pair<TKey, TValue>, IEnumerable<object>> Fields =
            item => new object[] {item.Key, item.Value};

        public Pair()
        {
        }

        public Pair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public TKey Key { get; set; }

        public TValue Value { get; set; }

        public override int GetHashCode()
        {
            return this.Equality().GetHashCode(Fields);
        }

        public override bool Equals(object obj)
        {
            return this.Equality().Equal(obj, Fields);
        }
    }
}