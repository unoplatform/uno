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
using System.Globalization;
using Uno.Extensions;
using System.Reflection;

namespace Uno.Conversion
{
	internal class CustomConversionStrategy<TFrom, TTo> : IConversionStrategy
	{
		private readonly Func<TFrom, CultureInfo, TTo> _conversion;

		public CustomConversionStrategy(Func<TFrom, CultureInfo, TTo> conversion)
		{
			_conversion = conversion;
		}

		public bool CanConvert(object value, Type toType, CultureInfo culture = null)
		{
			return value is TFrom && typeof(TTo).IsAssignableFrom(toType);
		}

		public object Convert(object value, Type toType, CultureInfo culture = null)
		{
			return _conversion((TFrom)value, culture);
		}
	}
}