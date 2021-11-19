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
#nullable enable

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information. 

using System;
using System.Threading;

namespace Uno.Disposables
{
    /// <summary>
    /// Represents a disposable resource that only disposes its underlying disposable resource when all <see cref="GetDisposable">dependent disposable objects</see> have been disposed.
    /// </summary>
    internal sealed class RefCountDisposable : ICancelable
    {
        private readonly bool _throwWhenDisposed;

        private IDisposable? _disposable;

        /// <summary>
        /// Holds the number of active child disposables and the
        /// indicator bit (31) if the main _disposable has been marked
        /// for disposition.
        /// </summary>
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefCountDisposable"/> class with the specified disposable.
        /// </summary>
        /// <param name="disposable">Underlying disposable.</param>
        /// <exception cref="ArgumentNullException"><paramref name="disposable"/> is null.</exception>
        public RefCountDisposable(IDisposable disposable) : this(disposable, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RefCountDisposable"/> class with the specified disposable.
        /// </summary>
        /// <param name="disposable">Underlying disposable.</param>
        /// <param name="throwWhenDisposed">Indicates whether subsequent calls to <see cref="GetDisposable"/> should throw when this instance is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="disposable"/> is null.</exception>
        public RefCountDisposable(IDisposable disposable, bool throwWhenDisposed)
        {
            _disposable = disposable ?? throw new ArgumentNullException(nameof(disposable));
            _count = 0;
            _throwWhenDisposed = throwWhenDisposed;
        }

        /// <summary>
        /// Gets a value that indicates whether the object is disposed.
        /// </summary>
        public bool IsDisposed => Volatile.Read(ref _count) == int.MinValue;

        /// <summary>
        /// Returns a dependent disposable that when disposed decreases the refcount on the underlying disposable.
        /// </summary>
        /// <returns>A dependent disposable contributing to the reference count that manages the underlying disposable's lifetime.</returns>
        /// <exception cref="ObjectDisposedException">This instance has been disposed and is configured to throw in this case by <see cref="RefCountDisposable(IDisposable, bool)"/>.</exception>
        public IDisposable GetDisposable()
        {
            // the current state
            var cnt = Volatile.Read(ref _count);

            for (; ; )
            {
                // If bit 31 is set and the active count is zero, don't create an inner
                if (cnt == int.MinValue)
                {
                    if (_throwWhenDisposed)
                    {
                        throw new ObjectDisposedException("RefCountDisposable");
                    }

                    return Disposable.Empty;
                }

                // Should not overflow the bits 0..30
                if ((cnt & 0x7FFFFFFF) == int.MaxValue)
                {
                    throw new OverflowException($"RefCountDisposable can't handle more than {int.MaxValue} disposables");
                }

                // Increment the active count by one, works because the increment
                // won't affect bit 31
                var u = Interlocked.CompareExchange(ref _count, cnt + 1, cnt);
                if (u == cnt)
                {
                    return new InnerDisposable(this);
                }
                cnt = u;
            }
        }

        /// <summary>
        /// Disposes the underlying disposable only when all dependent disposables have been disposed.
        /// </summary>
        public void Dispose()
        {
            var cnt = Volatile.Read(ref _count);

            for (; ; )
            {

                // already marked as disposed via bit 31?
                if ((cnt & 0x80000000) != 0)
                {
                    // yes, nothing to do
                    break;
                }

                // how many active disposables are there?
                var active = cnt & 0x7FFFFFFF;

                // keep the active count but set the dispose marker of bit 31
                var u = int.MinValue | active;

                var b = Interlocked.CompareExchange(ref _count, u, cnt);

                if (b == cnt)
                {
                    // if there were 0 active disposables, there can't be any more after
                    // the CAS so we can dispose the underlying disposable
                    if (active == 0)
                    {
                        _disposable?.Dispose();
                        _disposable = null;
                    }
                    break;
                }
                cnt = b;
            }
        }

        private void Release()
        {
            var cnt = Volatile.Read(ref _count);

            for (; ; )
            {
                // extract the main disposed state (bit 31)
                var main = (int)(cnt & 0x80000000);
                // get the active count
                var active = cnt & 0x7FFFFFFF;

                // keep the main disposed state but decrement the counter
                // in theory, active should be always > 0 at this point,
                // guaranteed by the InnerDisposable.Dispose's Exchange operation.
                System.Diagnostics.Debug.Assert(active > 0);
                var u = main | (active - 1);

                var b = Interlocked.CompareExchange(ref _count, u, cnt);

                if (b == cnt)
                {
                    // if after the CAS there was zero active disposables and
                    // the main has been also marked for disposing,
                    // it is safe to dispose the underlying disposable
                    if (u == int.MinValue)
                    {
                        _disposable?.Dispose();
                        _disposable = null;
                    }
                    break;
                }
                cnt = b;
            }
        }

        private sealed class InnerDisposable : IDisposable
        {
            private RefCountDisposable? _parent;

            public InnerDisposable(RefCountDisposable parent)
            {
                _parent = parent;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _parent, null)?.Release();
            }
        }
    }
}