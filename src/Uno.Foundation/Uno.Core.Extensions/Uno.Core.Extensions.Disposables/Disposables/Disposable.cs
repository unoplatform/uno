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

namespace Uno.Disposables
{
	/// <summary>
	/// Provides a set of static methods for creating Disposables.
	/// </summary>
	internal static class Disposable
	{
		/// <summary>
		/// Gets the disposable that does nothing when disposed.
		/// </summary>
		public static IDisposable Empty 
			=> DefaultDisposable.Instance;

		/// <summary>
		/// Creates a disposable object that invokes the specified action when disposed.
		/// </summary>
		/// <param name="dispose">Action to run during the first call to <see cref="IDisposable.Dispose"/>. The action is guaranteed to be run at most once.</param>
		/// <returns>The disposable object that runs the given action upon disposal.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="dispose"/> is null.</exception>
		public static IDisposable Create(Action dispose) 
			=> new AnonymousDisposable(dispose ?? throw new ArgumentNullException(nameof(dispose)));
	}
}
