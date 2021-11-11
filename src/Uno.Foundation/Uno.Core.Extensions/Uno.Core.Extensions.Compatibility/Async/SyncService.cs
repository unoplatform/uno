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

namespace Uno.Async
{
    internal class SyncService : IAsyncService
    {
        public static readonly IAsyncService Instance = new SyncService();

        #region IAsyncService Members

        public void Invoke<TResult>(
            Func<AsyncCallback, object, IAsyncResult> begin, 
            Func<IAsyncResult, TResult> end, 
            IObserver<TResult> callback)
        {
            var asyncResult = begin(null, null);

            if (asyncResult == null)
            {
                return;
            }

            try
            {
                callback.OnNext(end(asyncResult));
                callback.OnCompleted();
            }
            catch (Exception ex)
            {
                callback.OnError(ex);
            }
        }

        #endregion
    }
}
