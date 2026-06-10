#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Helpers;

public static class Annotations
{
#if WINAPPSDK
    public const DynamicallyAccessedMemberTypes IValueConverter_TargetTypeRequirements = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
#else   // WINAPPSDK
    public const DynamicallyAccessedMemberTypes IValueConverter_TargetTypeRequirements = Microsoft.UI.Xaml.Data.IValueConverter.TargetTypeRequirements;
#endif  // WINAPPSDK
}
