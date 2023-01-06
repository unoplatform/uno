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
using System.Reflection;
using Uno.Extensions;

namespace Uno.Conversion
{
	/// <summary>
	/// Will convert to and from enum, usually with strings
	/// </summary>
	/// <remarks>
	/// System.ComponentModel.DescriptionAttribute is can be used as result
	/// or parsing value, if not empty.
	/// A description attribute with "?" is considered default value.
	/// </remarks>
	internal sealed class EnumConversionStrategy : IConversionStrategy
	{
		private static Func<Type, string, CultureInfo, string> _findValue;

		static EnumConversionStrategy()
		{
			_findValue = SourceFindValue;
			_findValue = _findValue.AsLockedMemoized();
		}

		#region IConversionStrategy Members

		public bool CanConvert(object value, Type toType, CultureInfo culture = null)
		{
#if HAS_CRIPPLEDREFLECTION
			if (toType.GetTypeInfo().IsEnum)
#else
			if (toType.IsEnum)
#endif
			{
				// NOTE: To be tested
				var text = _findValue(toType, value as string ?? value.ToString(), culture);

				return text != null;
			}

			//TODO Support value == null
			return value != null &&
#if HAS_CRIPPLEDREFLECTION
				   value.GetType().GetTypeInfo().IsEnum &&
#else
				   value.GetType().IsEnum &&
#endif
				   toType == typeof(string);
		}

		public object Convert(object value, Type toType, CultureInfo culture = null)
		{
#if HAS_CRIPPLEDREFLECTION
			if (toType.GetTypeInfo().IsEnum)
#else
			if (toType.IsEnum)
#endif
			{
				// NOTE: To be tested
				var text = _findValue(toType, value as string ?? value.ToString(), culture);

				return Enum.Parse(toType, text, true);
			}

#if HAS_CRIPPLEDREFLECTION

			return value?.ToString(); ;
#else
			else
			{
				var text = value.ToString();

				var attribute = value.Reflection().GetDescriptor(text).FindAttribute<DescriptionAttribute>();

				return attribute == null ? text : attribute.Description;
			}
#endif
		}

		#endregion

		private static string SourceFindValue(Type enumType, string text, CultureInfo culture)
		{
			text = text.Trim();
			FieldInfo unkownFieldInfo = null;

#if false //!HAS_TYPEINFO && !HAS_CRIPPLEDREFLECTION && !WINDOWS_UWP
            var comparisonType =
                culture == null
                    ? StringComparison.InvariantCultureIgnoreCase
                    : StringComparison.CurrentCultureIgnoreCase;

            var fields = enumType.GetFields();
            using (culture == null ? null : new Localisation.CultureContext(culture))
#elif !HAS_TYPEINFO && HAS_CRIPPLEDREFLECTION
            var comparisonType =
				culture == null
					? StringComparison.OrdinalIgnoreCase
					: StringComparison.CurrentCultureIgnoreCase;

			var fields = enumType.GetTypeInfo().GetFields();
#else
			var comparisonType =
				culture == null
					? StringComparison.OrdinalIgnoreCase
					: StringComparison.CurrentCultureIgnoreCase;

			var fields = enumType.GetTypeInfo().DeclaredFields;
#endif
			{
				foreach (var field in fields)
				{
					if (field.Name.Equals(text, comparisonType))
					{
						return text;
					}

#if !HAS_CRIPPLEDREFLECTION
					var attribute = field.GetDescriptor().FindAttribute<DescriptionAttribute>();

					if (attribute == null)
					{
						continue;
					}

					if (attribute.Description.Equals(text, comparisonType))
					{
						return field.Name;
					}
					if (attribute.Description == "?")
					{
						unkownFieldInfo = field;
					}
#endif
				}
			}

			return unkownFieldInfo == null ? null : unkownFieldInfo.Name;
		}
	}
}
