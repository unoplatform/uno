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
using System.Reflection;

namespace Uno.Extensibility
{
	internal static class ExtensionManagerExtensions
	{
		public static TExtension FindOrCreateExtension<TExtension>(this IExtensionManager extensionManager, Func<TExtension> factory) where TExtension : IExtension
		{
			var found = extensionManager
								.GetRegisteredExtensions()
								.Where(extension => typeof(TExtension).IsAssignableFrom(extension.GetType()))
								.FirstOrDefault()
								.SelectOrDefault(x => (TExtension)(object)x);
			if (found == null)
			{
				found = factory();
				extensionManager.RegisterExtension(found);
			}
			return found;
		}

		public static TExtension GetRequiredExtension<TExtension>(this IExtensionManager extensionManager) where TExtension : IExtension
		{
			var found = extensionManager
								.GetRegisteredExtensions()
								.Where(extension => typeof(TExtension).IsAssignableFrom(extension.GetType()))
								.FirstOrDefault()
								.SelectOrDefault(x => (TExtension)(object)x);

			if (found == null)
			{
				throw new InvalidOperationException("Extension of type {0} has not been registered".InvariantCultureFormat(typeof(TExtension).FullName));
			}
			return found;
		}
	}

	internal interface IExtensionManager
	{
		object Target { get; }
		void RegisterExtension(IExtension extension);
		IEnumerable<IExtension> GetRegisteredExtensions();
	}

	internal interface IExtensionManager<TTarget> : IExtensionManager
	{
		new TTarget Target { get; }
		void RegisterExtension(IExtension<TTarget> extension);
		new IEnumerable<IExtension<TTarget>> GetRegisteredExtensions();
	}

	internal class ExtensionManager<TTarget> : IExtensionManager<TTarget>
	{
		private IList<IExtension<TTarget>> _extensions = new List<IExtension<TTarget>>();

		public ExtensionManager(TTarget target)
		{
			this.Target = target;
		}

		public void RegisterExtension(IExtension<TTarget> extension)
		{
			extension.ApplyTo(Target);
			_extensions.Add(extension);
		}

		public IEnumerable<IExtension<TTarget>> GetRegisteredExtensions()
		{
			return _extensions;
		}

		public TTarget Target
		{
			get;
			private set;
		}

		object IExtensionManager.Target
		{
			get { return this.Target; }
		}

		public void RegisterExtension(IExtension extension)
		{
			this.RegisterExtension((IExtension<TTarget>)extension);
		}

		IEnumerable<IExtension> IExtensionManager.GetRegisteredExtensions()
		{
			return GetRegisteredExtensions().Select(x => (IExtension)x);
		}
	}

	internal interface IExtension
	{
		void ApplyTo(object target);
	}

	internal interface IExtension<TTarget> : IExtension
	{
		void ApplyTo(TTarget target);
	}
}
