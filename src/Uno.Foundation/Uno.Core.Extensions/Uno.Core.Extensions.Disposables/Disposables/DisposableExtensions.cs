#nullable enable

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

namespace Uno.Disposables
{
	internal static class DisposableExtensions
	{
		/// <summary>
		/// Register an object who implements IDisposable to be disposed by a CompositeDisposable.
		/// </summary>
		/// <remarks>
		/// The parameter could be anything who implements ICollection&lt;IDisposable&gt;.
		/// This extension is designed for usage in a fluent declaration.
		/// </remarks>
		public static T? DisposeWith<T>(this T? disposable, ICollection<IDisposable> composite)
			where T : class, IDisposable
		{
			if (disposable != null)
			{
				composite.Add(disposable);
			}

			return disposable;
		}

		/// <summary>
		/// Register an object who implements IDisposable to be disposed by a SerialDisposable.
		/// </summary>
		/// <remarks>
		/// This extension is designed for usage in a fluent declaration.
		/// </remarks>
		public static T DisposeWith<T>(this T disposable, SerialDisposable serialDisposable)
			where T : class, IDisposable
		{
			if (serialDisposable == null)
			{
				throw new ArgumentNullException("serialDisposable");
			}

			serialDisposable.Disposable = disposable;

			return disposable;
		}

		/// <summary>
		/// Dispose the object if not null
		/// </summary>
		public static void SafeDispose(this IDisposable? disposable)
		{
			if (null != disposable)
			{
				disposable.Dispose();
			}
		}

		/// <summary>
		/// Dispose the object if not null and if it implements IDisposable
		/// </summary>
		/// <returns>
		/// True means the object was successfully disposed.
		/// </returns>
		public static bool TryDispose(this object maybeDisposableObject)
		{
			var disposable = maybeDisposableObject as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
				return true;
			}
			return false;
		}


		/// <summary>
		/// Dispose all items of an enumerable sequence
		/// <remarks>IF one dispose fails, continue other an raise a single Aggregate exception</remarks>
		/// </summary>
		/// <exception cref="AggregateException">Raised if any dispose fails</exception>
		public static void DisposeAll<T>(this IEnumerable<T> source)
			where T : IDisposable
		{
			var exceptions = new List<Exception>();
			foreach (var disposable in source)
			{
				try
				{
					disposable?.Dispose();
				}
				catch (Exception e)
				{
					exceptions.Add(e);
				}
			}

			if (exceptions.Any())
			{
				throw new AggregateException(exceptions);
			}
		}
	}
}
