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
using System.Security.Principal;
using System.Text;
using Uno.Extensions;

namespace Uno.Async
{
    internal class ActionServiceEntry
    {
        public Delegate Provider { get; set; }     
        public Delegate Action { get; set; }
    }

    internal class ActionService : IActionService
    {
        private List<ActionServiceEntry> entries = new List<ActionServiceEntry>();

#if HAS_WINDOWS_IDENTITY
		public WindowsIdentity Identity { get; set; }
#endif

        public void Register<T>(Func<T> provider, Action<T> action)
        {
            entries.Add(new ActionServiceEntry { Provider = provider, Action = action });
        }

        private IEnumerable<Action> PrepareActions()
        {
        	return
        		entries
        			.Select(entry =>
        			        	{
        			        		var invoke = entry.Provider.DynamicInvoke(null);
        			        		return Actions.Create(() => entry.Action.DynamicInvoke(invoke));
        			        	})
					.ToArray();
        }

		public IAsyncResult Begin(AsyncCallback callback, object asyncState, Action action)
        {
            return Begin<Null>(callback, asyncState, () =>
            {
                action();
                return null;
			});
        }

        public IAsyncResult Begin<T>(AsyncCallback callback, object asyncState, Func<T> selector)
        {
            //1-
            var actionsOnNewThread = PrepareActions();

            Func<T> selectorOnNewThread = () =>
                {
#if HAS_WINDOWS_IDENTITY
                    //2- 
					WindowsImpersonationContext context = null;
					try
					{
						if (Identity != null)
						{
							context = Identity.Impersonate();
						}
#endif

                    actionsOnNewThread.ForEach(x => x());
						return selector();
#if HAS_WINDOWS_IDENTITY
					}
					finally
					{
						if(context != null)
						{
							context.Dispose();
						}
					}
#endif
                };

            return selectorOnNewThread.BeginInvoke(
                r =>
                {
                    //3- 
                    if (callback != null)
                    {
                        callback(new AsyncResultDecorator(r, asyncState));
                    }
                },
                selectorOnNewThread);
        }

        public void End(IAsyncResult result)
        {
            End<Null>(result);
        }

        public T End<T>(IAsyncResult result)
        {
            //4- 
            var asyncResult = result as AsyncResultDecorator;

            if (asyncResult != null)
            {
                result = asyncResult.Inner;
            }

            Func<T> selector = result.AsyncState as Func<T>;

            return selector.EndInvoke(result);
        }
    }
}
