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
using System.Linq;
using System.Text;

namespace Uno.Disposables
{
	/// <summary>
	/// Represents a disposable object on which some external disposables can be linked to, in order to share its lifetime.
	/// <remarks>
	/// Default implementation:<br />
	/// <br />
	/// private readonly CompositeDisposable _subscriptions = new CompositeDisposable();<br />
	/// public IDisposable RegisterExtension(IDisposable disposable) => _subscriptions.DisposableAdd(disposable);<br />
	/// public void Dispose() => _subscriptions.Dispose();
	/// </remarks>
	/// </summary>
	internal interface IExtensibleDisposable : IDisposable
	{
		/// <summary>
		/// Currently registered extensions
		/// </summary>
		/// <remarks>
		/// To get a known service, use Extensions.<see cref="Enumerable.OfType{TExtension}"/>.
		/// </remarks>
		IReadOnlyCollection<object> Extensions { get; }

		/// <summary>
		/// Registers an extension and links its lifetime (disposal) with the lifetime of the extended disposable
		/// </summary>
		/// <remarks>
		/// The extension will be disposed with the extended disposable or when it's unregistered.
		/// To unregister, dispose the returned disposable.  You can discard the
		/// returned disposable if you're not planning to unregister the extension.
		/// </remarks>
		IDisposable RegisterExtension<T>(T extension) where T : class, IDisposable;
	}
}
