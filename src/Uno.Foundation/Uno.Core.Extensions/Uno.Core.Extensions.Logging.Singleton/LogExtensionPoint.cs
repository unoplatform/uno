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
using CommonServiceLocator;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Uno.Extensions
{
    internal static class LogExtensionPoint
    {
        private static ILoggerFactory? _loggerFactory;

		private static class Container<T>
        {
            internal static readonly ILogger Logger = AmbientLoggerFactory.CreateLogger<T>();
        }

		/// <summary>
		/// Retreives the <see cref="ILoggerFactory"/> for this the Uno extension point.
		/// </summary>
		public static ILoggerFactory AmbientLoggerFactory
		{
			get => Transactional.Update(ref _loggerFactory, l => l ?? GetFactory())!;
			set => Transactional.Update(ref _loggerFactory, l => value);
		}

		/// <summary>
		/// Gets a <see cref="ILogger"/> for the specified type.
		/// </summary>
		/// <param name="forType"></param>
		/// <returns></returns>
		public static ILogger Log(this Type forType) 
			=> AmbientLoggerFactory.CreateLogger(forType);

		/// <summary>
		/// Gets a logger instance for the current types
		/// </summary>
		/// <typeparam name="T">The type for which to get the logger</typeparam>
		/// <param name="instance"></param>
		/// <returns>A logger for the type of the instance</returns>
		public static ILogger Log<T>(this T instance) => Container<T>.Logger;

		private static ILoggerFactory GetFactory()
		{
			if (ServiceLocator.IsLocationProviderSet)
			{
				try
				{
					var service = ServiceLocator.Current.GetService(typeof(ILoggerFactory));

					if (service is ILoggerFactory factory)
					{
						return factory;
					}

					throw new InvalidOperationException($"The service {service?.GetType()} is not of type ILoggerFactory.");
				}
				catch (Exception e)
				{
					if (e is NullReferenceException || e is InvalidOperationException)
					{
#if HAS_CONSOLE
						Console.WriteLine("***** WARNING *****");
						Console.WriteLine("Unable to get the service locator ({0}), using the default logger to System.Diagnostics.Debug", e.Message);
#endif
						return new LoggerFactory();
					}
					else
					{
						throw;
					}
				}
			}
			else
			{
				return new LoggerFactory();
			}
		}
	}
}
