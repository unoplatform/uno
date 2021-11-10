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
    public class ListDecorator<T> : CollectionDecorator<T>, IList<T>
    {
        public ListDecorator()
        {
        }

        public ListDecorator(IList<T> target)
            : base(target)
        {
        }

        public IList<T> TargetList
        {
            get { return (IList<T>)base.Target; }
        }

        #region IList<T> Members

        public virtual int IndexOf(T item)
        {
            return TargetList.IndexOf(item);
        }

        public virtual void Insert(int index, T item)
        {
            TargetList.Insert(index, item);
        }

        public virtual void RemoveAt(int index)
        {
            TargetList.RemoveAt(index);
        }

        public virtual T this[int index]
        {
            get { return TargetList[index]; }
            set { TargetList[index] = value; }
        }

        #endregion
    }
}