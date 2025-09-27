#nullable enable

using System;
using System.Diagnostics;

namespace Windows.ApplicationModel.Core;

partial class CoreApplication
{
	private static void ExitPlatform() => Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
}
