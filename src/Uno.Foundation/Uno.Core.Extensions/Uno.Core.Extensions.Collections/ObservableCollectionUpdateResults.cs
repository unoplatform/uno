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
using System.Collections.Generic;

namespace Uno.Extensions
{
	/// <summary>
	/// Provides the results of a call to <see cref="ObservableCollectionExtensions.UpdateWithResults{T}(IList{T}, IEnumerable{T}, bool, IEqualityComparer{T})"/> 
	/// with details about what what was added, moved and removed.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class ObservableCollectionUpdateResults<T>
	{
		public ObservableCollectionUpdateResults(IEnumerable<T> added, IEnumerable<T> moved, IEnumerable<T> removed)
		{
			this.Added = added;
			this.Moved = moved;
			this.Removed = removed;
		}

		/// <summary>
		/// Gets the added items
		/// </summary>
		public IEnumerable<T> Added { get; }

		/// <summary>
		/// Gets the moved items
		/// </summary>
		public IEnumerable<T> Moved { get; }

		/// <summary>
		/// Gets the removed items
		/// </summary>
		public IEnumerable<T> Removed { get; }
	}
}
