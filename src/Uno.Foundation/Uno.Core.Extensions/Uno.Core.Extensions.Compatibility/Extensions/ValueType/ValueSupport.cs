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
using System.Collections.Generic;
using System.Reflection;
using Uno.Collections;

namespace Uno.Extensions.ValueType
{
    internal static class ValueSupport
    {
        //private static readonly SynchronizedDictionary<Type, IValueSupport> support;
        private static readonly IDictionary<Type, IValueSupport> support;

        static ValueSupport()
        {
            //support = new SynchronizedDictionary<Type, IValueSupport>(
            //    new Dictionary<Type, IValueSupport>()
            //    {
            //        { typeof(Int32), new Int32Support() },
            //        { typeof(Byte), new ByteSupport() }
            //    });
            support = new Dictionary<Type, IValueSupport>()
                {
                    { typeof(Int32), new Int32Support() },
                    { typeof(Byte), new ByteSupport() }
                };
        }

        public static IValueSupport<T> Get<T>()
        {
            var type = typeof (T);

#if WINDOWS_UWP
            if (type.GetTypeInfo().IsEnum)
#else
            if (type.IsEnum)
#endif
            {
				lock (typeof(ValueSupport))
				{
					//support.Lock.Write(
					//    d => d.ContainsKey(type), 
					//    d => d.Add(type, new EnumSupport<T>())
					if (!support.ContainsKey(type))
					{
						support.Add(type, new EnumSupport<T>());
					}
				}
            }

            return (IValueSupport<T>) Get(type);
        }

        public static IValueSupport Get(object instance)
        {
            return Get(instance.GetType());
        }

        public static IValueSupport Get(Type type)
        {
			lock (typeof(ValueSupport))
			{
				foreach (var kvp in support)
				{
					if (kvp.Key == type)
					{
						return kvp.Value;
					}
				}
			}

            throw new NotSupportedException();
        }
    }
}
