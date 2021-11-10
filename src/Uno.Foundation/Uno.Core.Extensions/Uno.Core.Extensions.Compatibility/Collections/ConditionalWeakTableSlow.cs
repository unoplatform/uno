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
using Uno.Extensions;
					
namespace Uno.Collections
{
	public class ConditionalWeakTableSlow<TKey, TValue>
		where TKey : class
		where TValue : class, new()
	{
		private SynchronizedDictionary<WeakReference, TValue> _source = new SynchronizedDictionary<WeakReference, TValue>();

		public TValue GetOrCreateValue(TKey owner)
		{
			TValue output = default(TValue);

			_source.Lock.Write(
				-1,
				s =>
				{
					var remove = new List<WeakReference>();

					var key = s.Keys.FirstOrDefault(r =>
					{
						if (r.Target == null)
						{
							remove.Add(r);
						}

						return object.ReferenceEquals(r.Target, owner);
					});

					// Cleanup old keys
					s.RemoveKeys(remove);

					if (key != null)
					{
						output = s[key];
						return true;
					}
					else
					{
						return false;
					}
				},
				s =>
				{
					output = s[new WeakReference(owner)] = new TValue();
				}
			);

			return output;
		}
	}
}
