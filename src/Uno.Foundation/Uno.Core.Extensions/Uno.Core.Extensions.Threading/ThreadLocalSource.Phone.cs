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
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Uno.Threading
{
	/// <summary>
	/// WARNING: This file is located here for compatibility reasons (SilverlightReaderWriterLock uses it, and it's in Uno.dll).
	/// Use Uno.Patterns.ThreadLocalSource<T> instead.
	/// </summary>
	internal sealed class ThreadLocalSource<T>
	{
		[ThreadStatic]
		private static T? _value;

		/// <summary>
		/// Initialize the ThreadLocalSource with no value for current thread (will be default(T))
		/// </summary>
		public ThreadLocalSource()
		{
		}

		/// <summary>
		/// Initialize the ThreadLocalSource with a starting value
		/// </summary>
		/// <param name="value"></param>
		public ThreadLocalSource(T value)
		{
			// TODO: We should not call virtual members in the contructor
			Value = value;
		}

		public T? Value
		{
			get { return _value; }
			set { _value = value; }
		}
	}
}
