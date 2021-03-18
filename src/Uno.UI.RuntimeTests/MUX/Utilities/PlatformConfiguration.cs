// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.System.Profile;

namespace Common
{
    public enum DeviceType
    {
        Desktop,
        Phone,
        OneCore
    }

    public enum OSVersion : ushort
    {
        Threshold2 = 2,
        Redstone1 = 3,
        Redstone2 = 4,
        Redstone3 = 5,
        Redstone4 = 6,
        Redstone5 = 7,
        NineteenH1 = 8
    }

    public class PlatformConfiguration
    {
        const OSVersion MaxOSVersion = OSVersion.Redstone2;

        private const ushort InvalidAPIVersion = 255;
        private static ushort _currentAPIVersion = InvalidAPIVersion;

        private static bool IsApiContractPresent(ushort version)
        {
            return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", version);
        }

        public static ushort GetCurrentAPIVersion()
        {
            if (_currentAPIVersion == InvalidAPIVersion)
            {
                ushort currentAPIVersion = 3;
                while (IsApiContractPresent((ushort)(currentAPIVersion + 1)))
                {
                    currentAPIVersion++;
                }

                _currentAPIVersion = currentAPIVersion;
            }
            return _currentAPIVersion;
        }

        public static bool IsDevice(DeviceType type)
        {
            var deviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;

            if (type == DeviceType.Desktop && deviceFamily == "Windows.Desktop")
            {
                return true;
            }
            else if (type == DeviceType.Phone && deviceFamily == "Windows.Mobile")
            {
                return true;
            }
            else if (type == DeviceType.OneCore && deviceFamily != "Windows.Desktop" && deviceFamily != "Windows.Mobile")
            {
                return true;
            }

            return false;
        }

        public static bool IsOsVersion(OSVersion version)
        {
            // We can determine the OS version by checking for the presence of the Universal contract
            // corresonding to that version and the absense of the contract version corresonding to
            // the next OS version.
            return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", (ushort)version) &&
                ((version == MaxOSVersion) || !ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", (ushort)(version + 1)));
        }

        public static bool IsOsVersionGreaterThan(OSVersion version)
        {
            // We can determine that the OS version is greater than the specified version by checking for
            // the presence of the Universal contract corresonding to the next version.
            return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", (ushort)(version + 1));
        }

        public static bool IsOsVersionGreaterThanOrEqual(OSVersion version)
        {
            return IsOsVersionGreaterThan(version) || IsOsVersion(version);
        }

        public static bool IsOSVersionLessThan(OSVersion version)
        {
            return !IsOsVersionGreaterThanOrEqual(version);
        }
    }
}
