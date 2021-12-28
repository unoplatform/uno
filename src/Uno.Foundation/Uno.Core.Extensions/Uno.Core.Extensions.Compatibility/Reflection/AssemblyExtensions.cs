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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.Extensions
{
    internal static class AssemblyExtensions
    {
        public static Version GetVersionNumber(this Assembly assembly)
        {
            // Note: Assembly.GetExecutingAssembly().GetName() is not accessible in WP7
            return new Version(Regex.Match(assembly.FullName, @"(\d+)(.\d+)(.\d+)?(.\d+)?").ToString());
        }

		public static string GetProductName(this Assembly assembly)
		{
			var productNameAttribute = assembly.GetAssemblyAttribute<AssemblyProductAttribute>();
			return productNameAttribute == null ? null : productNameAttribute.Product;
		}

    	public static string GetCopyright(this Assembly assembly)
		{
			var assemblyCopyrightAttribute = assembly.GetAssemblyAttribute<AssemblyCopyrightAttribute>();
			return assemblyCopyrightAttribute == null ? null : assemblyCopyrightAttribute.Copyright;
		}

#if NETFX_CORE
		public static T GetAssemblyAttribute<T>(this Assembly assembly) where T : Attribute
		{
			return assembly.GetCustomAttribute(typeof(T)) as T;
		}
#else
		public static T GetAssemblyAttribute<T>(this Assembly assembly) where T : Attribute
		{
			return (T)Attribute.GetCustomAttribute(assembly, typeof(T));
		}
#endif
	} 
}
