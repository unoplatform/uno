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
using System.IO;
using System.Linq;
using System.Text;
#if WINDOWS_PHONE
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Info;
#endif
using Uno.Extensions;
using System.Net.NetworkInformation;

namespace Uno
{
	/// <summary>
	/// Runtime information helper class
	/// </summary>
	internal class Runtime
	{
		private static readonly char[] InvalidPathChars;

		static Runtime()
		{
			var invalidPathChars = Path.GetInvalidPathChars().ToList();
			invalidPathChars.AddRange(new char[] { '\\', '/', ':' });
			InvalidPathChars = invalidPathChars.ToArray();
		}

		public static bool IsIsolatedStorageUri(Uri uri)
		{
			return uri.OriginalString.IndexOf("ms-appdata:///Local/", StringComparison.OrdinalIgnoreCase) >= 0
				|| uri.OriginalString.IndexOf("isostore:", StringComparison.OrdinalIgnoreCase) >= 0;

			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns true if the uri is a local uri (isolated storage, linked as content, resource, etc.)
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public static bool IsLocalUri(Uri uri)
		{
			throw new NotImplementedException("This has not been analyzed / implemented for the current platform");
		}

		/// <summary>
		/// Returns a safe filename for a string token.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string GetSafeFilename(string text)
		{
			StringBuilder builder = new StringBuilder(text.Length);
			foreach (char c in text)
			{
				builder.Append(InvalidPathChars.Contains(c) ? '_' : c);
			}
			return builder.ToString();
		}

		public static Uri GetAppContentUri(string path)
		{
			if (!path.StartsWith("/", StringComparison.Ordinal))
			{
				throw new ArgumentException("Path should start with /");
			}
			throw new NotImplementedException();
		}

		public static string GetIsolatedStoragePath(Uri isolatedStorageUri)
		{
			throw new NotImplementedException();
		}

		public static Uri GetIsolatedStorageUri(string path, bool isTile)
		{
			if (!path.StartsWith("/", StringComparison.Ordinal))
			{
				throw new ArgumentException("Path should start with /");
			}
			else
			{
				path = path.Substring(1);
			}
			throw new NotImplementedException();
		}

		public static bool IsNetworkAvailable()
		{
			return NetworkInterface.GetIsNetworkAvailable();
		}
	}

	internal static class RuntimeExtensions
	{
		public static bool IsLocal(this Uri uri)
		{
			return Runtime.IsLocalUri(uri);
		}
	}
}
