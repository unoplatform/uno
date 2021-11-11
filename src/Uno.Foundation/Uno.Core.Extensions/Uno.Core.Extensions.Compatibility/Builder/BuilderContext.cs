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
using Uno.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;

namespace Uno.Builder
{
	/// <summary>
	/// A context that helps the creation of an IBuilder based builder.
	/// </summary>
	/// <typeparam name="TOwner"></typeparam>
	internal class BuilderContext<TOwner> : BuilderContext
	{
		public BuilderContext(TOwner owner)
		{
			Owner = owner;
		}

		public TOwner Owner { get; private set; }
	}

	/// <summary>
	/// A context that helps the creation of an IBuilder based builder.
	/// </summary>
	/// <typeparam name="TOwner"></typeparam>
	internal class BuilderContext
	{
		private static WeakAttachedDictionary<object, object> _attached = new WeakAttachedDictionary<object, object>();

		private readonly List<Action> _buildActions = new List<Action>();

		/// <summary>
		/// Builds this builder dependencies
		/// </summary>
		public void BuildDependencies()
		{
			_buildActions.ForEach(a => a());
		}

		/// <summary>
		/// Appends an action to be executed after this builder has been built
		/// </summary>
		/// <param name="action"></param>
		public void AppendBuildDependency(Action action)
		{
			_buildActions.Add(action);
		}

		/// <summary>
		/// Creates a builder context for the specified owner
		/// </summary>
		/// <typeparam name="TOwner"></typeparam>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static BuilderContext<TOwner> Create<TOwner>(TOwner owner)
		{
			return new BuilderContext<TOwner>(owner);
		}

		/// <summary>
		/// Gets the specified builder on the specified owner instance
		/// </summary>
		/// <param name="owner">The instance on which the new builder will be attached</param>
		/// <param name="name">The named instance</param>
		/// <param name="builder">The selector that will create a new builder instance</param>
		/// <returns></returns>
		public static TBuilder GetOrCreateBuilder<TBuilder>(object owner, string name, Func<TBuilder> builder)
			where TBuilder : IBuilder
		{
			return _attached.GetValue(owner, name, builder);
		}

		/// <summary>
		/// Gets the specified builder on the specified owner instance
		/// </summary>
		/// <param name="owner">The instance on which the new builder will be attached</param>
		/// <param name="name">The named instance</param>
		/// <param name="builder">The selector that will create a new builder instance</param>
		/// <param name="selector">The builder select that will configure the builder</param>
		/// <returns></returns>
		public static TBuilder GetOrCreateBuilder<TOwner, TBuilder>(TOwner owner, string name, Func<TBuilder, TBuilder> selector, Func<TBuilder> builder)
			where TBuilder : IBuilder
			where TOwner : IBuilder
		{
			var instance = _attached.GetValue(
				owner,
				name,
				() =>
				{
					var b = builder();
					owner.AppendBuild(() => b.Build());
					return b;
				}
			);

			selector(instance);

			return instance;
		}
	}
}
