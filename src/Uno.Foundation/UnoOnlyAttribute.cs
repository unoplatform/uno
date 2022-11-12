using System;

namespace Uno;

/// <summary>
/// This member is only available in Uno Platform and not part of the UWP/WinUI contract.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
public sealed class UnoOnlyAttribute : Attribute
{
}
