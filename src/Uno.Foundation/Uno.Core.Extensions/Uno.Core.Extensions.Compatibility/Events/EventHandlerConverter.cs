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
using System.Collections.Immutable;
using System.Text;

namespace Uno.Events
{
	/// <summary>
	/// A helper to convert the type of an event handler and manage the subscriptions.
	/// </summary>
	/// <typeparam name="TFromHandler">The source handler</typeparam>
	/// <typeparam name="TToHandler">The target handler</typeparam>
	internal class EventHandlerConverter<TFromHandler, TToHandler>
	{
		private ImmutableDictionary<TFromHandler, TToHandler> _handlers = ImmutableDictionary<TFromHandler, TToHandler>.Empty;

		private readonly Func<TFromHandler, TToHandler> _convert;
		private readonly Action<TToHandler> _add;
		private readonly Action<TToHandler> _remove;

		public EventHandlerConverter(
			Func<TFromHandler, TToHandler> convert,
			Action<TToHandler> add,
			Action<TToHandler> remove)
		{
			_convert = convert;
			_add = add;
			_remove = remove;
		}

		/// <summary>
		/// Subscribe to the inner event
		/// </summary>
		public void Add(TFromHandler from)
		{
			var to = _convert(from);
			Transactional.Update(ref _handlers, h => h.Add(from, to));
			_add(to);
		}

		/// <summary>
		/// Unsubscribe from the inner event
		/// </summary>
		public void Remove(TFromHandler from)
		{
			TToHandler to;
			if (Transactional.TryRemove(ref _handlers, from, out to))
			{
				_remove(to);
			}
		}
	}
}
