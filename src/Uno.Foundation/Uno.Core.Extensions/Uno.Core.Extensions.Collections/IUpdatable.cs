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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno
{
	/// <summary>
	/// Identifies an object that can get refreshed from another object of the same type. 	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks> This is useful for items bound to the view. When a newer instance with its Equals
	/// returning true for the old item comes in play, the old item is kept, but updated from that
	/// new instance.</remarks>
    internal interface IUpdatable<T>
    {
		Task UpdateAsync(CancellationToken ct, T newerInstance);
    }
}
