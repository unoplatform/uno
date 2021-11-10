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

namespace Uno.Threading
{
    public interface ISynchronizableLock<T>
    {
        TValue Read<TValue>(int millisecondsTimeout, Func<T, TValue> read);

        TValue Write<TValue>(int millisecondsTimeout, Func<T, TValue> write);

		/// <summary>
		/// Performs a write operation if the read operation return false.
		/// </summary>
		/// <param name="millisecondsTimeout">The timeout to acquire the write lock</param>
		/// <param name="read">A lambda that will test if the data can be read</param>
		/// <param name="write">A lambda that will perform the write if the read failed</param>
		/// <returns>true if the read succeeded, otherwise false.</returns>
        bool Write(int millisecondsTimeout, Func<T, bool> read, Action<T> write);

        IDisposable CreateReaderScope(int millisecondsTimeout);

        IDisposable CreateWriterScope(int millisecondsTimeout);
    }
}