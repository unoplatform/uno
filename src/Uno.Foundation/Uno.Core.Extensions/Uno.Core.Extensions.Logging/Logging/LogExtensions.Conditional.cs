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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Logging
{
    internal static partial class LogExtensions
    {
        /// <summary>
        /// Adds a debug log, but only if debug logs are enabled. If they're not, the provided messageDelegate
        /// lambda will not be called.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="messageSelector"></param>
        /// <param name="error"></param>
        public static void DebugIfEnabled(this ILogger log, Func<string> messageSelector, Exception? error = null)
        {
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(0, error, messageSelector(), error);
            }
        }

        /// <summary>
        /// Adds an info log, but only if info logs are enabled. If they're not, the provided messageDelegate
        /// lambda will not be called.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="messageSelector"></param>
        /// <param name="error"></param>
        public static void InfoIfEnabled(this ILogger log, Func<string> messageSelector, Exception? error = null)
        {
            if (log.IsEnabled(LogLevel.Information))
            {
                log.LogInformation(0, error, messageSelector(), error);
            }
        }

        /// <summary>
        /// Adds a warninglog, but only if warning logs are enabled. If they're not, the provided messageDelegate
        /// lambda will not be called.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="messageSelector"></param>
        /// <param name="error"></param>
        public static void WarnIfEnabled(this ILogger log, Func<string> messageSelector, Exception? error = null)
        {
            if (log.IsEnabled(LogLevel.Warning))
            {
                log.LogWarning(0, error, messageSelector(), error);
            }
        }

        /// <summary>
        /// Adds an error log, but only if error logs are enabled. If they're not, the provided messageDelegate
        /// lambda will not be called.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="messageSelector"></param>
        /// <param name="error"></param>
        public static void ErrorIfEnabled(this ILogger log, Func<string> messageSelector, Exception? error = null)
        {
            if (log.IsEnabled(LogLevel.Error))
            {
                log.LogError(0, error, messageSelector(), error);
            }
        }

        /// <summary>
        /// Adds a critical error log, but only if fatal error logs are enabled. If they're not, the provided messageDelegate
        /// lambda will not be called.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="messageSelector"></param>
        /// <param name="error"></param>
        public static void CriticalIfEnabled(this ILogger log, Func<string> messageSelector, Exception? error = null)
        {
            if (log.IsEnabled(LogLevel.Critical))
            {
                log.LogCritical(0, error, messageSelector(), error);
            }
        }
    }
}
