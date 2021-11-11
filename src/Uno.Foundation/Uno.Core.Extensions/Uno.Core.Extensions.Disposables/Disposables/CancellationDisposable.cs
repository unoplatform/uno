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

namespace Uno.Disposables
{
    /// <summary>
    /// Represents a disposable resource that has an associated <seealso cref="T:System.Threading.CancellationToken"/> that will be set to the cancellation requested state upon disposal.
    /// </summary>
    internal sealed class CancellationDisposable : ICancelable
    {
        private readonly CancellationTokenSource _cts;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Uno.Disposables.CancellationDisposable"/> class that uses an existing <seealso cref="T:System.Threading.CancellationTokenSource"/>.
        /// </summary>
        /// <param name="cts"><seealso cref="T:System.Threading.CancellationTokenSource"/> used for cancellation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="cts"/> is null.</exception>
        public CancellationDisposable(CancellationTokenSource cts)
        {
            if (cts == null)
                throw new ArgumentNullException("cts");

            _cts = cts;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Uno.Disposables.CancellationDisposable"/> class that uses a new <seealso cref="T:System.Threading.CancellationTokenSource"/>.
        /// </summary>
        public CancellationDisposable()
            : this(new CancellationTokenSource())
        {
        }

        /// <summary>
        /// Gets the <see cref="T:System.Threading.CancellationToken"/> used by this CancellationDisposable.
        /// </summary>
        public CancellationToken Token
        {
            get { return _cts.Token; }
        }

        /// <summary>
        /// Cancels the underlying <seealso cref="T:System.Threading.CancellationTokenSource"/>.
        /// </summary>
        public void Dispose()
        {
            _cts.Cancel();
        }

        /// <summary>
        /// Gets a value that indicates whether the object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return _cts.IsCancellationRequested; }
        }
    }
}