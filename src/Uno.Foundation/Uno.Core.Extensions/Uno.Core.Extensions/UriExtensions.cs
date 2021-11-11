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
using System.Text;

namespace Uno.Extensions
{
	internal static class UriExtensions
	{
		private const int EscapeDataStringCharactersMaxLength = 10000;

		/// <summary>
		/// Converts a string to its escaped representation.
		/// This extension bypasses the Uri.EscapeDataString characters limit.
		/// </summary>
		/// Source: http://stackoverflow.com/questions/6695208/uri-escapedatastring-invalid-uri-the-uri-string-is-too-long
		internal static string EscapeDataString(string value)
		{
			var sb = new StringBuilder();
			var loops = value.Length / EscapeDataStringCharactersMaxLength;

			for (var i = 0; i <= loops; i++)
			{
				if (i < loops)
				{
					sb.Append(Uri.EscapeDataString(value.Substring(EscapeDataStringCharactersMaxLength * i, EscapeDataStringCharactersMaxLength)));
				}
				else
				{
					sb.Append(Uri.EscapeDataString(value.Substring(EscapeDataStringCharactersMaxLength * i)));
				}
			}

			return sb.ToString();
		}
	}
}
