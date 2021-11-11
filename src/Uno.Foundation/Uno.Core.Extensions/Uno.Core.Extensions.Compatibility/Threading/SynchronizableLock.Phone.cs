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
using System.Threading;
using Uno.Extensions;
using System.Runtime.Serialization;

namespace Uno.Threading
{
    internal class SynchronizableExclusiveLock<T> : ISynchronizableLock<T>
    {
        private readonly T instance;
		private object _gate = new object();

		public SynchronizableExclusiveLock(T instance)
        {
            this.instance = instance;
        }

        #region ISynchronizableLock<T> Members

        public virtual TValue Read<TValue>(int millisecondsTimeout, Func<T, TValue> read)
        {
            lock(_gate)
            {
                return read(instance);
            }
        }

        public virtual bool Write(int millisecondsTimeout, Func<T, bool> read, Action<T> write)
        {
            lock(_gate)
            {
                if (read(instance))
                {
                    return true;
                }
                else
                {
                    if (read(instance))
                    {
                        return true;
                    }
                    else
                    {
                        write(instance);
                    }
                }

                return false;
            }
        }

        public virtual TValue Write<TValue>(int millisecondsTimeout, Func<T, TValue> write)
        {
			lock (_gate)
			{
                return write(instance);
            }
        }

        public virtual IDisposable CreateReaderScope(int millisecondsTimeout)
        {
			Monitor.Enter(_gate);

			return Actions.Create(() => Monitor.Exit(_gate)).ToDisposable();
        }

        public virtual IDisposable CreateWriterScope(int millisecondsTimeout)
        {
			Monitor.Enter(_gate);

			return Actions.Create(() => Monitor.Exit(_gate)).ToDisposable();
		}

        #endregion
    }
}
