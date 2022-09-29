#nullable disable

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 2019/04/12 (Jerome Laban <jerome.laban@nventive.com>):
//	- Extracted from dotnet.exe
//

using System;
using System.Globalization;
using System.IO;

namespace Uno.UI.SourceGenerators.Telemetry.PersistenceChannel
{
    internal static class PersistenceChannelDebugLog
    {
        private static readonly bool _isEnabled = IsEnabledByEnvironment();

		private static bool IsEnabledByEnvironment()
		{
			if (bool.TryParse(Environment.GetEnvironmentVariable("UNO_SOURCEGEN_ENABLE_PERSISTENCE_CHANNEL_DEBUG_OUTPUT"), out var enabled))
			{
				return enabled;
			}

			return false;
        }

        public static void WriteLine(string message)
        {
            if (_isEnabled)
            {
                Console.WriteLine(message);
            }
        }

        internal static void WriteException(Exception exception, string format, params string[] args)
        {
            var message = string.Format(CultureInfo.InvariantCulture, format, args);
            WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} Exception: {1}", message, exception.ToString()));
        }
    }
}
