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
using Uno.Validation;

namespace Uno.Extensions
{
	internal static class ValidationExtensions
	{
		public static ValidationExtensionPoint<T> Validation<T>(this T value) where T : class
		{
			return new ValidationExtensionPoint<T>(value);
		}

		public static T NotNull<T>(this ValidationExtensionPoint<T> extensionPoint, string name) where T : class
		{
			if (extensionPoint.ExtendedValue == null)
			{
				throw new ArgumentNullException(name);
			}

			return extensionPoint.ExtendedValue;
		}
		public static T Found<T>(this ValidationExtensionPoint<T> extensionPoint) where T : class
		{
			if (extensionPoint.ExtendedValue == null)
			{
				throw new ArgumentException();
				//throw new NotFoundException();
			}

			return extensionPoint.ExtendedValue;
		}
		public static void NotSupported<T>(this ValidationExtensionPoint<T> extensionPoint) where T : class
		{
			throw new NotSupportedException();
		}

		public static T IsTrue<T>(this ValidationExtensionPoint<T> extensionPoint, Func<T, bool> validation, string paramName, string message = null) where T : class
		{
			if (!validation(extensionPoint.ExtendedValue))
			{
				throw new ArgumentException(message);
			}

			return extensionPoint.ExtendedValue;
		}

		public static T IsFalse<T>(this ValidationExtensionPoint<T> extensionPoint, Func<T, bool> validation, string paramName, string message = null) where T : class
		{
			if (validation(extensionPoint.ExtendedValue))
			{
				throw new ArgumentException(message);
			}

			return extensionPoint.ExtendedValue;
		}
	}
}
