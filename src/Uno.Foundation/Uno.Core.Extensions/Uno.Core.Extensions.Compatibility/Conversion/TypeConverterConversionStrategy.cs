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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Uno.Extensions;

namespace Uno.Conversion
{
	internal class TypeConverterConversionStrategy : IConversionStrategy
	{
		#region IConversionStrategy Members

		public bool CanConvert(object value, Type toType, CultureInfo culture = null)
		{
			if (value == null)
			{
				return true;
			}

#if HAS_NOTYPEDESCRIPTOR || WINDOWS_UWP
			return false;
#else
			return TypeDescriptor.GetConverter(value.GetType()).CanConvertTo(toType) ||
				   TypeDescriptor.GetConverter(toType).CanConvertFrom(value.GetType());
#endif
		}

		public object Convert(object value, Type toType, CultureInfo culture = null)
		{
			if (value == null)
			{
				return null;
			}

#if HAS_NOTYPEDESCRIPTOR || WINDOWS_UWP
			throw new InvalidOperationException("TypeConverterConversionStrategy should never return true for CanConvert under WinRT.");
#else
			var valueTypeConverter = TypeDescriptor.GetConverter(value.GetType());

			var canconvert = valueTypeConverter.CanConvertTo(toType);
			if (canconvert)
			{
				return valueTypeConverter.ConvertTo(null, culture, value, toType);
			}
			else
			{
				if (toType == typeof(float))
				{
					float fValue;
					if (float.TryParse(value.ToString(), NumberStyles.Float, culture, out fValue))
					{
						return fValue;
					}

					if (float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out fValue))
					{
						return fValue;
					}
				}

				if (toType == typeof(decimal))
				{
					decimal dValue;
					if (decimal.TryParse(value.ToString(), NumberStyles.AllowDecimalPoint, culture, out dValue))
					{
						return dValue;
					}
					if (decimal.TryParse(value.ToString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out dValue))
					{
						return dValue;
					}
				}

				return TypeDescriptor.GetConverter(toType).ConvertFrom(null, culture, value);
			}
#endif

		}

		#endregion
	}
}
