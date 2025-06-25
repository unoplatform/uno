// Copyright (c) 2015-2016 Xamarin, Inc.
// Copyright (c) 2017-2018 Microsoft Corporation.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// https://github.com/mono/SkiaSharp/blob/main/binding/Binding.Shared/LibraryLoader.cs

using System;
using System.Runtime.InteropServices;

namespace Uno.WinUI.Runtime.Skia.X11;

internal static class LibDl
{
	private const string SystemLibrary = "libdl.so";

	private const string SystemLibrary2 = "libdl.so.2"; // newer Linux distros use this

	private const int RTLD_LAZY = 1;
	private const int RTLD_NOW = 2;
	private const int RTLD_DEEPBIND = 8;

	private static bool UseSystemLibrary2 = true;

	public static IntPtr dlopen(string path, bool lazy = true)
	{
		try
		{
			return dlopen2(path, (lazy ? RTLD_LAZY : RTLD_NOW) | RTLD_DEEPBIND);
		}
		catch (DllNotFoundException)
		{
			UseSystemLibrary2 = false;

			return dlopen1(path, (lazy ? RTLD_LAZY : RTLD_NOW) | RTLD_DEEPBIND);
		}
	}

	public static IntPtr dlsym(IntPtr handle, string symbol)
	{
		return UseSystemLibrary2 ? dlsym2(handle, symbol) : dlsym1(handle, symbol);
	}

	public static void dlclose(IntPtr handle)
	{
		if (UseSystemLibrary2)
		{
			dlclose2(handle);
		}
		else
		{
			dlclose1(handle);
		}
	}

	[DllImport(SystemLibrary, EntryPoint = "dlopen")]
	private static extern IntPtr dlopen1(string path, int mode);

	[DllImport(SystemLibrary, EntryPoint = "dlsym")]
	private static extern IntPtr dlsym1(IntPtr handle, string symbol);

	[DllImport(SystemLibrary, EntryPoint = "dlclose")]
	private static extern void dlclose1(IntPtr handle);

	[DllImport(SystemLibrary2, EntryPoint = "dlopen")]
	private static extern IntPtr dlopen2(string path, int mode);

	[DllImport(SystemLibrary2, EntryPoint = "dlsym")]
	private static extern IntPtr dlsym2(IntPtr handle, string symbol);

	[DllImport(SystemLibrary2, EntryPoint = "dlclose")]
	private static extern void dlclose2(IntPtr handle);
}
