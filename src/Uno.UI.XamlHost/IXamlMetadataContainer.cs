// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Win32.UI.XamlApplication/XamlApplication.idl

using System.Collections.Generic;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.XamlHost;

/// <summary>
/// Represents a container for XAML metadata providers.
/// Usually this would be a class derived from Application.
/// </summary>
public interface IXamlMetadataContainer
{
	/// <summary>
	/// Gets XAML metadata providers.
	/// </summary>
	IList<IXamlMetadataProvider> MetadataProviders { get; }
}
