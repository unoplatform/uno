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

namespace Uno.Collections
{
    public class DictionnaryCompositeDisposable<TKey> : Dictionary<TKey, IDisposable>, IDisposable
    {
        ~DictionnaryCompositeDisposable()
        {
            Dispose();
        }

        public void SafeAdd(TKey key, IDisposable value)
        {
            DisposeValue(key);
            Add(key, value);
        }

        public void DisposeValue(TKey key)
        {
            var value = this.UnoGetValueOrDefault(key);

            if (value != null)
            {
                this.Remove(key);
                value.Dispose();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            foreach (var kvp in this)
            {
                kvp.Value.Dispose();
            }
            Clear();
        }
    }
}
