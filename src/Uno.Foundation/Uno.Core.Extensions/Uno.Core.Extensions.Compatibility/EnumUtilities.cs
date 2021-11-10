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
using Uno.Extensions;
using Uno.Reflection;

namespace Uno
{
	public static class EnumUtilities
	{
		static readonly Func<Type, bool, System.Tuple<string, object, object>[]> _getValues = GetValues;

		static EnumUtilities()
		{
			_getValues = _getValues.AsLockedMemoized();
		}

		/// <summary>
		/// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
		/// </summary>
		/// <param name="enumType">An enumeration type.</param>
		/// <param name="value">A string containing the name or value to convert.</param>
		/// <param name="ignoreCase">true to ignore case; false to regard case.</param>
		/// <param name="useFallbackValue">If true and parsing failed, try to find the value which has the <seealso cref="FallbackValueAttribute"/> and send it back.</param>
		/// <returns>An object of type enumType whose value is represented by value.</returns>
		public static object Parse(Type enumType, string value, bool ignoreCase, bool useFallbackValue)
		{
			object result;
			if(!TryParse(enumType, value, ignoreCase, out result))
			{
				if(useFallbackValue)
				{
					var fallbackValue = GetValuesWithAttributes<FallbackValueAttribute>(enumType, ignoreCase).FirstOrDefault();

					if (fallbackValue != null)
					{
						result = fallbackValue;
					}
					else
					{
						throw new ArgumentException("Value '{0}' is not part of the enumeration of type '{1}'".InvariantCultureFormat(value, enumType.ToString()));
					}
				}
				else
				{
					throw new ArgumentException("Value '{0}' is not part of the enumeration of type '{1}'".InvariantCultureFormat(value, enumType.ToString()));
				}
			}
			return result;
		}

		public static T ParseOrDefault<T>(string value, bool ignoreCase, T defaultValue)
		{
			object result;
			return TryParse(typeof (T), value, ignoreCase, out result) ? (T) result : defaultValue;
		}

		public static bool TryParse<TEnum>(string value, bool ignoreCase, out TEnum result) 
			where TEnum : struct
		{
			return Enum.TryParse(value, ignoreCase, out result);
		}

		public static bool TryParse(Type enumType, string value, bool ignoreCase, out object result)
		{
			var tuple = _getValues(enumType, ignoreCase)
				.FirstOrDefault(v =>
				{
					if (ignoreCase)
					{
						return v.Item1.ToUpperInvariant() == value.ToUpperInvariant() || 
								v.Item3.ToString().ToUpperInvariant() == value.ToUpperInvariant();
					}
					else
					{
						return v.Item1.ToString() == value || 
								v.Item3.ToString() == value;
					}
				});

			if(tuple != null)
			{
				result = tuple.Item2;
				return true;
			}

			result = null;
            return false;
		}

		public static System.Tuple<string, object, object>[] GetValues(Type enumType, bool ignoreCase)
		{
			return enumType
				.GetFields()
				.Where(x => x.IsLiteral)
				.Select(info =>
				{
					var enumValue = info.GetValue(null);
					var enumParsedValue = Enum.Parse(enumType, info.Name, ignoreCase);
					var enumUnderlyingValue = Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()), CultureInfo.InvariantCulture);
					return System.Tuple.Create(info.Name, enumParsedValue, enumUnderlyingValue);
                })
				.ToArray();
		}

		public static object[] GetValuesWithAttributes<TAttribute>(Type enumType, bool ignoreCase)
		{
			var result = new List<object>();

			foreach (var fieldInfo in enumType.GetFields())
			{
				var info = fieldInfo;

				var attributes = fieldInfo.GetCustomAttributes(typeof(TAttribute),false).FirstOrDefault();

				if (attributes != default(object))
				{
					result.Add(Enum.Parse(enumType, info.Name, ignoreCase));
				}
			}

			return result.ToArray();
		}

#if !SILVERLIGHT && !NETFX_CORE
		public static FieldInfo[] GetValuesWithAttributesReflectionOnly<TAttribute>(Type enumType, bool ignoreCase)
		{
			return enumType
				.GetFields()
				.Where(fieldInfo => fieldInfo.GetCustomAttributesData().Any(attr => attr.Constructor.DeclaringType.FullName == typeof(TAttribute).FullName))
				.ToArray();
		}
#endif

//#if WINDOWS_PHONE
//        public static Array GetValues(this Enum thisEnum, Type enumType)
//        {
//            var values = new List<object>();

//            enumType.GetFields().ForEach(x => values.Add(((FieldInfo)x).GetValue(value)));

//            return values.ToArray();
//        }
//#endif
	}
}
