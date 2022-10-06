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
using System.IO;

namespace Uno.Extensions
{
	internal static class TextWriterExtensions
	{
		public static void Write(this TextWriter writer, string format, params object[] args)
		{
			writer.Write(string.Format(CultureInfo.InvariantCulture, format, args));
		}

		public static void WriteLine(this TextWriter writer, string format, params object[] args)
		{
			writer.Write(string.Format(CultureInfo.InvariantCulture, format, args));
		}

		public static void Write(this TextWriter writer, IFormatProvider formatProvider, string format, params object[] args)
		{
			writer.Write(string.Format(formatProvider, format, args));
		}

		public static void WriteLine(this TextWriter writer, IFormatProvider formatProvider, string format, params object[] args)
		{
			writer.Write(string.Format(formatProvider, format, args));
		}
	}
}
