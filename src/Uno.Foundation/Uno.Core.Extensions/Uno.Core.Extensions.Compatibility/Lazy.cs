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
using System.Threading.Tasks;

namespace Uno
{
    [Legacy("NV0049")]
	public static class Lazy
    {
        public static T Get<T>(ref T value, Func<T> factory)
			where T : class
        {
            if (value == null)
            {
                value = factory();
            }

            return value;
        }

		public static T Get<T>(ref T value)
			where T : class
		{
			return Get(ref value, () => default(T));
		}

		public static T FindOrCreate<T>(ref T value)
			where T : class, new()
		{
			return Get(ref value, () => new T());
		}
    }
}
