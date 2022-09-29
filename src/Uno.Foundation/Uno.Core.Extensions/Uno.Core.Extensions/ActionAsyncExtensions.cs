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

namespace Uno.Extensions
{
	/// <summary>
	/// Extensions of <see cref="ActionAsync"/>
	/// </summary>
    internal static class ActionAsyncExtensions
    {
		/// <summary>
		/// Invoke the <paramref name="action"/> if not null.
		/// </summary>
		/// <param name="action">Action to invoke</param>
		/// <param name="ct">A CanellationToken</param>
		/// <returns></returns>
		public static async Task SafeInvoke(this ActionAsync action, CancellationToken ct)
		{
			if (action != null)
			{
				await action(ct);
			}
		}

		/// <summary>
		/// Invoke the <paramref name="action"/> if not null.
		/// </summary>
		/// <param name="action">Action to invoke</param>
		/// <param name="ct">A CanellationToken</param>
		/// <param name="param">Parameter of action</param>
		/// <returns></returns>
		public static async Task SafeInvoke<TParam>(this ActionAsync<TParam> action, CancellationToken ct, TParam param)
		{
			if (action != null)
			{
				await action(ct, param);
			}
		}
	}
}
