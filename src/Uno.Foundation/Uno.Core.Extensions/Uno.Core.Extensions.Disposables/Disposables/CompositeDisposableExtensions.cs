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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Disposables
{
    internal static class CompositeDisposableExtensions
	{
		/// <summary>
		/// Adds an action to be executed when the CompositeDisposable is disposed.
		/// </summary>
		/// <param name="disposable">The composite disposable instance</param>
		/// <param name="action">The action to execute</param>
		/// <returns>The source composite disposable</returns>
		public static CompositeDisposable Add(this CompositeDisposable disposable, Action action)
		{
			disposable.Add(Disposable.Create(action));

			return disposable;
		}
	}
}
