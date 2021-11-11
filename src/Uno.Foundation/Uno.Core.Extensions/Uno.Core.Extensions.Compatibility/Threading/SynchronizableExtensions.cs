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
using Uno.Threading;

namespace Uno.Extensions
{
    internal static class SynchronizableExtensions
    {
        public static void Read<T>(this ISynchronizableLock<T> sync, Action<T> read)
        {
            Read<T, Null>(sync, item =>
                                    {
                                        read(item);
                                        return null;
                                    });
        }

        public static void Write<T>(this ISynchronizableLock<T> sync, Action<T> write)
        {
            Write<T, Null>(sync, item =>
                                     {
                                         write(item);
                                         return null;
                                     });
        }

        public static TValue Write<T, TValue>(this ISynchronizableLock<T> sync, Func<T, TValue> write)
        {
            return sync.Write(-1, write);
        }

        public static TValue Read<T, TValue>(this ISynchronizableLock<T> sync, Func<T, TValue> read)
        {
            return sync.Read(-1, read);
        }

		/// <summary>
		/// Performs a write operation if the read operation returns false.
		/// </summary>
		/// <param name="read">A lambda that will test if the data can be read</param>
		/// <param name="write">A lambda that will perform the write if the read failed</param>
		/// <returns>true if the read succeeded, otherwise false.</returns>
		public static bool Write<T>(this ISynchronizableLock<T> sync, Func<T, bool> read, Action<T> write)
        {
            return sync.Write(-1, read, write);
        }

        public static IDisposable CreateReaderScope<T>(this ISynchronizableLock<T> sync)
        {
            return sync.CreateReaderScope(-1);
        }

        public static IDisposable CreateWriterScope<T>(this ISynchronizableLock<T> sync)
        {
            return sync.CreateWriterScope(-1);
        }
    }
}