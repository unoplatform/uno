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
using System.Security.Principal;

namespace Uno.Async
{
    internal interface IActionService
    {
        void Register<T>(Func<T> provider, Action<T> action);

		IAsyncResult Begin(AsyncCallback callback, object asyncState, Action action);

		IAsyncResult Begin<T>(AsyncCallback callback, object asyncState, Func<T> selector);

#if HAS_WINDOWS_IDENTITY
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification="Security feature")]
		WindowsIdentity Identity { set; }
#endif

        void End(IAsyncResult result);

        T End<T>(IAsyncResult result);
    }
}
