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

namespace Uno.Disposables
{
	/// <summary>
	/// Represents a disposable resource whose underlying disposable resource can be replaced by another disposable resource, causing automatic disposal of the previous underlying disposable resource.
	/// </summary>
	internal sealed class SerialDisposable : ICancelable
	{
		private readonly object _gate = new object();
		private IDisposable? _current;
		private bool _disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Uno.Disposables.SerialDisposable"/> class.
		/// </summary>
		public SerialDisposable()
		{
		}

		/// <summary>
		/// Gets a value that indicates whether the object is disposed.
		/// </summary>
		public bool IsDisposed
		{
			get
			{
				lock (_gate)
				{
					return _disposed;
				}
			}
		}

		/// <summary>
		/// Gets or sets the underlying disposable.
		/// </summary>
		/// <remarks>If the SerialDisposable has already been disposed, assignment to this property causes immediate disposal of the given disposable object. Assigning this property disposes the previous disposable object.</remarks>
		public IDisposable? Disposable
		{
			get
			{
				return _current;
			}

			set
			{
				var shouldDispose = false;
				var old = default(IDisposable);
				lock (_gate)
				{
					shouldDispose = _disposed;
					if (!shouldDispose)
					{
						old = _current;
						_current = value;
					}
				}
				if (old != null)
					old.Dispose();
				if (shouldDispose && value != null)
					value.Dispose();
			}
		}

		/// <summary>
		/// Disposes the underlying disposable as well as all future replacements.
		/// </summary>
		public void Dispose()
		{
			var old = default(IDisposable);

			lock (_gate)
			{
				if (!_disposed)
				{
					_disposed = true;
					old = _current;
					_current = null;
				}
			}

			if (old != null)
				old.Dispose();
		}
	}
}
