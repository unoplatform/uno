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
using Uno.Decorator;
using Uno.Extensions;

namespace Uno.Collections
{
    /// <summary>
    /// Adapts a collection of type T into a collection of type U
    /// </summary>
    /// <typeparam name="T">Original type</typeparam>
    /// <typeparam name="U">Target type</typeparam>
    public class CollectionAdapter<T, U> : Decorator<ICollection<T>>, ICollection<U>
    {
        /// <summary>
        /// Constructs a CollectionAdapter
        /// </summary>
        /// <param name="target">Collection of type T to adapt.</param>
        /// <param name="from">Function used to adapt a U into a T.</param>
        /// <param name="to">Function used to adapt a T into a U.</param>
        public CollectionAdapter(ICollection<T> target, Func<U, T> from, Func<T, U> to)
            : base(target.Validation().NotNull("target"))
        {
            From = from.Validation().NotNull("from");
            To = to.Validation().NotNull("to");
        }

        public Func<U, T> From { get; private set; }

        public Func<T, U> To { get; private set; }

        /// <summary>
        /// Returns the adapter.
        /// </summary>
        protected virtual IEnumerable<U> Enumerable
        {
            get { return Target.Select(To); }
        }

        #region ICollection<U> Members

        /// <summary>
        /// See base.
        /// </summary>
        /// <param name="item"></param>
        public virtual void Add(U item)
        {
            Target.Add(From(item));
        }

        /// <summary>
        /// See base.
        /// </summary>
        public virtual void Clear()
        {
            Target.Clear();
        }

        /// <summary>
        /// See base.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual bool Contains(U item)
        {
            return Target.Contains(From(item));
        }

        /// <summary>
        /// See base.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public virtual void CopyTo(U[] array, int arrayIndex)
        {
            Array.Copy(Enumerable.ToArray(), 0, array, arrayIndex, Count);
        }

        /// <summary>
        /// See base.
        /// </summary>
        public virtual int Count
        {
            get { return Target.Count; }
        }

        /// <summary>
        /// See base.
        /// </summary>
        public virtual bool IsReadOnly
        {
            get { return Target.IsReadOnly; }
        }

        /// <summary>
        /// See base.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual bool Remove(U item)
        {
            return Target.Remove(From(item));
        }

        /// <summary>
        /// See base.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator<U> GetEnumerator()
        {
            return Enumerable.GetEnumerator();
        }

        /// <summary>
        /// See base.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}