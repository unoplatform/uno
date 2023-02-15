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

namespace Uno
{
	/// <summary>
	/// An empty implementation of the IDisposable class.
	/// </summary>
	internal class NullDisposable : IDisposable
	{
		/// <summary>
		/// Provider for a instance of the NullDisposable
		/// </summary>
		public static readonly IDisposable Instance = new NullDisposable();

		/// <summary>
		/// Private constructor, use Instance.
		/// </summary>
		private NullDisposable()
		{
		}

		#region IDisposable Members

		/// <summary>
		/// See IDisposable.
		/// </summary>
		public void Dispose()
		{
		}

		#endregion
	}
}
