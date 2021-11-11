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

namespace Uno.Collections
{
    internal class ListAdapter<T, U> : CollectionAdapter<T, U>, IList<U>
    {
        public ListAdapter(IList<T> target, Func<U, T> from, Func<T, U> to)
            : base(target, from, to)
        {
        }

        protected new IList<T> Target
        {
            get { return (IList<T>) base.Target; }
        }

        #region IList<U> Members

        /// <summary>
        /// See base.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual int IndexOf(U item)
        {
            return Target.IndexOf(From(item));
        }

        /// <summary>
        /// See base.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, U item)
        {
            Target.Insert(index, From(item));
        }

        /// <summary>
        /// See base.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            Target.RemoveAt(index);
        }

        /// <summary>
        /// See base.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public U this[int index]
        {
            get { return To(Target[index]); }
            set { Target[index] = From(value); }
        }

        #endregion
    }
}