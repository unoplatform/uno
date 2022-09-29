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
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using _Task = System.Threading.Tasks.Task;

namespace Uno
{
    internal static class Actions
    {
        /// <summary>
        /// An action which does nothing.
        /// </summary>
        public static readonly Action Null = () => { };

        /// <summary>
        /// An ActionAsync which does nothing.
        /// </summary>
        public static readonly ActionAsync NullAsync = _ => NullTask;
        internal static readonly Task NullTask = _Task.FromResult(true);

        public static Action Create(Action action)
        {
            return action;
        }

        public static ActionAsync CreateAsync(ActionAsync action)
        {
            return action;
        }

        public static Action<T> Create<T>(Action<T> action)
        {
            return action;
        }

        public static ActionAsync<T> CreateAsync<T>(ActionAsync<T> action)
        {
            return action;
        }

        /// <summary>
        /// Creates an action that will only execute once the provided action, even if called multiple times. This is Thread Safe.
        /// </summary>
        /// <param name="action">The action to be executed once</param>
        /// <returns>An action.</returns>
        public static Action CreateOnce(Action action)
        {
            var once = 0;

            return () =>
            {
                if (Interlocked.Exchange(ref once, 1) == 0)
                {
                    action();
                }
            };
        }

        public static IDisposable ToDisposable(Action action)
        {
            return action.ToDisposable();
        }
    }
}