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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

#if NET6_0_OR_GREATER && __IOS__
using ObjCRuntime;
#endif

namespace Uno.Conversion
{
	internal class PrimitiveConversionStrategy : IConversionStrategy
	{
		public bool CanConvert(object value, Type toType, CultureInfo culture = null)
		{
			var targetNullableType = Nullable.GetUnderlyingType(toType);
			if (targetNullableType != null)
			{
				return CanConvert(value, targetNullableType, culture);
			}

			var isTargetPrimitive = toType
#if WINDOWS_UWP
				.GetTypeInfo()
#endif
				.IsPrimitive || toType == typeof(string);
			bool isSourcePrimitive = IsPrimitive(value);

			return isTargetPrimitive && isSourcePrimitive;
		}

		private static bool IsPrimitive(object value)
		{
			var isSourcePrimitive = value.GetType()
#if WINDOWS_UWP
										.GetTypeInfo()
#endif
										.IsPrimitive
									|| value is string
#if XAMARIN_IOS
									// Those are platform primitives provided for 64 bits compatibility
									// with iOS 8.0 and later
									|| value is nfloat
									|| value is nint
									|| value is nuint
#endif
									;
			return isSourcePrimitive;
		}

		public object Convert(object value, Type toType, CultureInfo culture = null)
		{
			var targetNullableType = Nullable.GetUnderlyingType(toType);
			if (targetNullableType != null)
			{
				return Convert(value, targetNullableType, culture);
			}

			return System.Convert.ChangeType(value, toType, culture ?? CultureInfo.InvariantCulture);
		}
	}
}
