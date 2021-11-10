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
using System.IO;
using System.Reflection;

namespace Uno.Resources
{
	public static class EmbeddedResourceFactory
	{
		public static Stream Get(Assembly assembly, string fullName)
		{
			return assembly.GetManifestResourceStream(fullName);
		}

#if !WINDOWS_UWP
		public static Stream Get<T>(Assembly assembly, string key)
		{
			return assembly.GetManifestResourceStream(typeof (T), key);
		}

		public static Stream Get<T>(string key)
		{
			return Get<T>(Assembly.GetCallingAssembly(), key);
		}

		public static Stream Get(string fullName)
		{
			return Get(Assembly.GetCallingAssembly(), fullName);
		}
#endif
	}
}
