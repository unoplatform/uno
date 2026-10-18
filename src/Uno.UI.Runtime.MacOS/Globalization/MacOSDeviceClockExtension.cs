using System.Runtime.InteropServices;
using Uno.Foundation.Extensibility;
using Windows.Globalization;

namespace Uno.UI.Runtime.MacOS;

// Reads the current locale through CoreFoundation, which, unlike .NET's culture, carries the 24-hour time setting.
internal sealed partial class MacOSDeviceClockExtension : IDeviceClockExtension
{
	private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

	private static readonly MacOSDeviceClockExtension _instance = new();

	public static void Register() => ApiExtensibility.Register(typeof(IDeviceClockExtension), _ => _instance);

	public string? GetLocaleTag()
	{
		var locale = CFLocaleCopyCurrent();
		try
		{
			return ToString(CFLocaleGetIdentifier(locale));
		}
		finally
		{
			Release(locale);
		}
	}

	public unsafe string? GetHourFormat()
	{
		var locale = CFLocaleCopyCurrent();
		var template = IntPtr.Zero;
		var format = IntPtr.Zero;
		try
		{
			var j = 'j';
			template = CFStringCreateWithCharacters(IntPtr.Zero, &j, 1);
			format = CFDateFormatterCreateDateFormatFromTemplate(IntPtr.Zero, template, 0, locale);
			return ToString(format);
		}
		finally
		{
			Release(format);
			Release(template);
			Release(locale);
		}
	}

	private static unsafe string? ToString(IntPtr cfString)
	{
		if (cfString == IntPtr.Zero)
		{
			return null;
		}

		var length = CFStringGetLength(cfString);
		var buffer = new char[length];
		fixed (char* chars = buffer)
		{
			CFStringGetCharacters(cfString, new CFRange(0, length), chars);
		}

		return new string(buffer);
	}

	private static void Release(IntPtr cf)
	{
		if (cf != IntPtr.Zero)
		{
			CFRelease(cf);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private readonly struct CFRange(nint location, nint length)
	{
		public readonly nint Location = location;
		public readonly nint Length = length;
	}

	[LibraryImport(CoreFoundation)]
	private static partial IntPtr CFLocaleCopyCurrent();

	[LibraryImport(CoreFoundation)]
	private static partial IntPtr CFLocaleGetIdentifier(IntPtr locale);

	[LibraryImport(CoreFoundation)]
	private static unsafe partial IntPtr CFStringCreateWithCharacters(IntPtr allocator, char* chars, nint length);

	[LibraryImport(CoreFoundation)]
	private static partial IntPtr CFDateFormatterCreateDateFormatFromTemplate(IntPtr allocator, IntPtr template, nuint options, IntPtr locale);

	[LibraryImport(CoreFoundation)]
	private static partial nint CFStringGetLength(IntPtr cfString);

	[LibraryImport(CoreFoundation)]
	private static unsafe partial void CFStringGetCharacters(IntPtr cfString, CFRange range, char* buffer);

	[LibraryImport(CoreFoundation)]
	private static partial void CFRelease(IntPtr cf);
}
