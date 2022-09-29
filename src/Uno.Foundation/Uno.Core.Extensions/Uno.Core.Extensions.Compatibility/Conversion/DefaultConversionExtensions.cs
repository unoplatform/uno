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
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace Uno.Conversion
{
    internal sealed class DefaultConversionExtensions : IConversionExtensions
    {
        private readonly List<IConversionStrategy> _registrations;
        private readonly List<IConversionStrategy> _fallbackRegistrations;

        public DefaultConversionExtensions(bool includeDefaultFallbackStrategies = true)
        {
            _registrations = new List<IConversionStrategy>(4);
            _fallbackRegistrations = new List<IConversionStrategy>(4);

            if (includeDefaultFallbackStrategies)
            {
                this.RegisterFallbackStrategy<EnumConversionStrategy>();
                this.RegisterFallbackStrategy<TypeConverterConversionStrategy>();
                this.RegisterFallbackStrategy<PrimitiveConversionStrategy>();
            }
        }

        public void RegisterStrategy(IConversionStrategy strategy)
        {
            _registrations.Add(strategy);
        }

        public void RegisterFallbackStrategy(IConversionStrategy strategy)
        {
            _fallbackRegistrations.Add(strategy);
        }

        public bool CanConvert(ConversionExtensionPoint extensionPoint, object value, Type toType, CultureInfo culture = null)
        {
            return GetConversionStrategy(value, toType, culture) != null;
        }

        public ConversionExtensionPoint Conversion(object value)
        {
            return new ConversionExtensionPoint(value);
        }

        public object To(ConversionExtensionPoint extensionPoint, Type toType, CultureInfo culture = null)
        {
            if (extensionPoint == null || extensionPoint.ExtendedValue == null)
            {
                throw new ArgumentNullException("extensionPoint", "No extended value to convert");
            }

            return To(extensionPoint.ExtendedValue, toType, culture);
        }

        public object To(object value, Type toType, CultureInfo culture = null)
        {
            var strategy = GetConversionStrategy(value, toType, culture);

            if (strategy == null)
            {
                var message =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No strategy registration for conversion '{0}' to '{1}'",
                        value.GetType().Name,
                        toType.Name);

                throw new ArgumentOutOfRangeException("extensionPoint", message);
            }

            return strategy.Convert(value, toType, culture);
        }

        private IConversionStrategy GetConversionStrategy(object value, Type toType, CultureInfo culture)
        {
            return _registrations
                .Concat(_fallbackRegistrations)
                .FirstOrDefault(item => item.CanConvert(value, toType, culture));
        }
    }
}
