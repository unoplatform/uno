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

    internal class SynchronizableLock<T> : ISynchronizableLock<T>
    {
        private readonly T instance;

        //TODO Use Slim Lock?
        private ReaderWriterLockSlim readerWriter = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public SynchronizableLock(T instance)
        {
            this.instance = instance;
        }

        #region ISynchronizableLock<T> Members

        public virtual TValue Read<TValue>(int millisecondsTimeout, Func<T, TValue> read)
        {
            if (readerWriter.TryEnterUpgradeableReadLock(millisecondsTimeout))
            {
                try
                {
                    return read(instance);
                }
                finally
                {
                    readerWriter.ExitUpgradeableReadLock();
                }
            }

            throw new TimeoutException("Failed to acquire read lock"); 
        }

        public virtual bool Write(int millisecondsTimeout, Func<T, bool> read, Action<T> write)
        {
            if (readerWriter.TryEnterUpgradeableReadLock(millisecondsTimeout))
            {

                try
                {
                    if (read(instance))
                    {
                        return true;
                    }
                    else
                    {
                        if (readerWriter.TryEnterWriteLock(millisecondsTimeout))
                        {

                            try
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
                            finally
                            {
                                readerWriter.ExitWriteLock();
                            }
                        }
                        else
                        {
                            throw new TimeoutException("Failed to acquire write lock");
                        }
                    }
                }
                finally
                {
                    readerWriter.ExitUpgradeableReadLock();
                }

                return false;
            }

            throw new TimeoutException("Failed to acquire read lock");
        }

        public virtual TValue Write<TValue>(int millisecondsTimeout, Func<T, TValue> write)
        {
            if (readerWriter.TryEnterWriteLock(millisecondsTimeout))
            {
                try
                {
                    return write(instance);
                }
                finally
                {
                    readerWriter.ExitWriteLock();
                }
            }

            throw new TimeoutException("Failed to acquire write lock");
        }

        public virtual IDisposable CreateReaderScope(int millisecondsTimeout)
        {
            if (readerWriter.TryEnterUpgradeableReadLock(millisecondsTimeout))
            {
                return Actions.Create(() => readerWriter.ExitUpgradeableReadLock()).ToDisposable();
            }

            throw new TimeoutException("Failed to acquire read lock");
        }

        public virtual IDisposable CreateWriterScope(int millisecondsTimeout)
        {
            if (readerWriter.TryEnterWriteLock(millisecondsTimeout))
            {
                return Actions.Create(() => readerWriter.ExitWriteLock()).ToDisposable();
            }

            throw new TimeoutException("Failed to acquire read lock");
        }

        #endregion

        #region Serialization Specific

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if(readerWriter == null)
                readerWriter = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }    
        #endregion
    }
}
