#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.ApplicationModel.Core;
using Uno.Foundation.Extensibility;
using System.Runtime.InteropServices.JavaScript;
using static __Windows.ApplicationModel.Core.CoreApplicationNative;
using System.Diagnostics;

namespace Windows.ApplicationModel.Core;

partial class CoreApplication
{
	private static ICoreApplicationExtension? _coreApplicationExtension;

	static partial void InitializePlatform()
	{
		NativeInitialize();

		if (ApiExtensibility.CreateInstance(typeof(CoreApplication), out _coreApplicationExtension))
		{
		}
	}
}
