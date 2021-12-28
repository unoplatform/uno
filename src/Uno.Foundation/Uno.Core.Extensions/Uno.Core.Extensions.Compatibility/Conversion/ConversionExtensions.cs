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
using Uno.Conversion;
using System.Globalization;

namespace Uno.Extensions
{
	internal static class ConversionExtensions
	{
		public static IConversionExtensions Extensions { get; } = new DefaultConversionExtensions();

		public static ConversionExtensionPoint Conversion(this object instance)
		{
			return Extensions.Conversion(instance);
		}

		public static bool CanConvert(this ConversionExtensionPoint extensionPoint, Type toType, CultureInfo culture = null)
		{
			return Extensions.CanConvert(extensionPoint, toType, toType, culture);
		}

		public static T To<T>(this ConversionExtensionPoint extensionPoint, CultureInfo culture = null)
		{
			return Extensions.To<T>(extensionPoint, culture);
		}

		public static object To(this ConversionExtensionPoint extensionPoint, Type toType, CultureInfo culture = null)
		{
			return Extensions.To(extensionPoint, toType, culture);
		}
	}
}
