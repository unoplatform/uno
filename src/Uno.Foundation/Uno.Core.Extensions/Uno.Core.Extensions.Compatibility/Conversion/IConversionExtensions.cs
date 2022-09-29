#nullable disable

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

namespace Uno.Conversion
{
	internal interface IConversionExtensions
	{
		/// <summary>
		/// Create extension point (prefer the usage of method extensions)
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		ConversionExtensionPoint Conversion(object value);

		/// <summary>
		/// Register a conversion strategy
		/// </summary>
		void RegisterStrategy(IConversionStrategy strategy);

		/// <summary>
		/// Register a conversion strategy to be used as fallback
		/// </summary>
		void RegisterFallbackStrategy(IConversionStrategy strategy);

		/// <summary>
		/// Check if it's possible to do a conversion
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		bool CanConvert(ConversionExtensionPoint extensionPoint, object value, Type toType, CultureInfo culture = null);

		/// <summary>
		/// Initiate the conversion
		/// </summary>
		/// <remarks>
		/// This method is usually called by the extension point.
		/// </remarks>
		/// <returns>Conversion result</returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		object To(ConversionExtensionPoint extensionPoint, Type toType, CultureInfo culture = null);
	}

	internal static class ConversionExtensionsExtensions
	{
		/// <summary>
		/// Fluently register a conversion strategy
		/// </summary>
		public static IConversionExtensions RegisterStrategy<T>(this IConversionExtensions conversionExtensions) where T : IConversionStrategy, new()
		{
			conversionExtensions.RegisterStrategy(new T());
			return conversionExtensions;
		}

		/// <summary>
		/// Fluently register a custom conversion strategy
		/// </summary>
		public static IConversionExtensions RegisterCustomStrategy<TFrom, TTo>(
			this IConversionExtensions conversionExtensions, Func<TFrom, CultureInfo, TTo> conversion)
		{
			conversionExtensions.RegisterStrategy(new CustomConversionStrategy<TFrom, TTo>(conversion));
			return conversionExtensions;
		}

		/// <summary>
		/// Fluently register a custom conversion strategy
		/// </summary>
		public static IConversionExtensions RegisterCustomStrategy<TFrom, TTo>(
			this IConversionExtensions conversionExtensions, Func<TFrom, TTo> conversion)
		{
			conversionExtensions.RegisterStrategy(new CustomCulturelessConversionStrategy<TFrom, TTo>(conversion));
			return conversionExtensions;
		}

		/// <summary>
		/// Fluently register a conversion strategy to be used as fallback
		/// </summary>
		public static IConversionExtensions RegisterFallbackStrategy<T>(this IConversionExtensions conversionExtensions) where T : IConversionStrategy, new()
		{
			conversionExtensions.RegisterFallbackStrategy(new T());
			return conversionExtensions;
		}

		/// <summary>
		/// Convert source to a specified generic type
		/// </summary>
		/// <returns>Conversion result</returns>
		public static T To<T>(this IConversionExtensions conversionExtensions, ConversionExtensionPoint extensionPoint, CultureInfo culture = null)
		{
			return (T)conversionExtensions.To(extensionPoint, typeof(T), culture);
		}

		//public static IConversionExtensions RegisterStrategy
	}
}
