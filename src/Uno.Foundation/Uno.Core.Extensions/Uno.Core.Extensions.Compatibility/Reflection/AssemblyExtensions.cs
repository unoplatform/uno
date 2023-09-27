// ******************************************************************
// Copyright � 2015-2018 Uno Platform Inc. All rights reserved.
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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.Extensions
{
	internal static partial class AssemblyExtensions
	{
		public static Version GetVersionNumber(this Assembly assembly)
		{
			// Note: Assembly.GetExecutingAssembly().GetName() is not accessible in WP7
			return new Version(VersionMatch().Match(assembly.FullName).ToString());
		}

#if NET7_0_OR_GREATER
		[GeneratedRegex(@"(\d+)(.\d+)(.\d+)?(.\d+)?")]
		private static partial Regex VersionMatch();
#else
		private static Regex VersionMatch()
			=> new Regex(@"(\d+)(.\d+)(.\d+)?(.\d+)?");
#endif
	}
}
