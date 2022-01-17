using System;
using System.Linq;
using Windows.Devices.Input;

namespace Uno.Testing;

/// <summary>
/// Specify the type of pointer to use for that test.
/// WARNING: This has no effects on UI tests, cf. remarks.
/// </summary>
/// <
/// <remarks>You can add this attributes multiple times to run the test with multiple pointer types.</remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple= true)]
public class InjectedPointerAttribute : Attribute
{
	public PointerDeviceType Type { get; }

	public InjectedPointerAttribute(PointerDeviceType type)
	{
		Type = type;
	}
}

public interface IInjectPointers
{
	public IDisposable SetPointer(PointerDeviceType type);
}
