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
using System.Collections;
using System.Collections.Generic;
using Uno.Decorator;

namespace Uno.Collections
{
    public class CollectionDecorator<T> : Decorator<ICollection<T>>, ICollection<T>
    {
        public CollectionDecorator()
            : this(new List<T>())
        {
        }

        public CollectionDecorator(ICollection<T> target)
            : base(target)
        {
        }

        #region ICollection<T> Members

        public virtual void Add(T item)
        {
            Target.Add(item);
        }

        public virtual void Clear()
        {
            Target.Clear();
        }

        public virtual bool Contains(T item)
        {
            return Target.Contains(item);
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            Target.CopyTo(array, arrayIndex);
        }

        public virtual int Count
        {
            get { return Target.Count; }
        }

        public virtual bool IsReadOnly
        {
            get { return Target.IsReadOnly; }
        }

        public virtual bool Remove(T item)
        {
            return Target.Remove(item);
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return Target.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}