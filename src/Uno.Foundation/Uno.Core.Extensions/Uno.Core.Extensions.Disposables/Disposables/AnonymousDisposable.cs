#nullable enable

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
	/// Represents an Action-based disposable.
	/// </summary>
	internal sealed class AnonymousDisposable : ICancelable
	{
		private volatile Action? _dispose;

		/// <summary>
		/// Constructs a new disposable with the given action used for disposal.
		/// </summary>
		/// <param name="dispose">Disposal action which will be run upon calling Dispose.</param>
		public AnonymousDisposable(Action dispose)
		{
			System.Diagnostics.Debug.Assert(dispose != null);

			_dispose = dispose;
		}

		/// <summary>
		/// Gets a value that indicates whether the object is disposed.
		/// </summary>
		public bool IsDisposed => _dispose == null;

		/// <summary>
		/// Calls the disposal action if and only if the current instance hasn't been disposed yet.
		/// </summary>
		public void Dispose()
		{
#pragma warning disable 0420
			var dispose = Interlocked.Exchange(ref _dispose, null);
#pragma warning restore 0420
			if (dispose != null)
			{
				dispose();
			}
		}
	}
}
