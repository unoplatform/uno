#nullable disable

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

namespace Uno.Extensions
{
    internal class ExtensionPoint<T> : IExtensionPoint<T>
    {
        private readonly Type type;
        private readonly T value;

        public ExtensionPoint(T value)
        {
            this.value = value;
        }

        public ExtensionPoint(Type type)
        {
            this.type = type;
        }

        #region IExtensionPoint<T> Members

        public T ExtendedValue
        {
            get { return value; }
        }

        object IExtensionPoint.ExtendedValue
        {
            get { return value; }
        }

        public Type ExtendedType
        {
            // TODO: value might not be null
            get { return type ?? (value == null ? typeof (T) : value.GetType()); }
        }

        #endregion
    }
}