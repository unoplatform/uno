// ******************************************************************
// Copyright � 2015-2018 Uno Platform Inc. All rights reserved.
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
#if NET6_0_OR_GREATER
using global::System.Threading.Tasks;
using global::System;

namespace Uno
{
	internal record AsyncDisposableAction(Func<Task> Action) : IAsyncDisposable
	{
		public Func<Task> Action { get; private set; } = Action;

		public async ValueTask DisposeAsync()
			=> await Action();
	}
}
#endif
