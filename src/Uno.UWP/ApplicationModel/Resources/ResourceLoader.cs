#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Globalization;

namespace Windows.ApplicationModel.Resources;

public sealed partial class ResourceLoader
{
	public static ResourceLoader GetForCurrentView() => GetOrCreateNamedResourceLoader(DefaultResourceLoaderName);

	public static ResourceLoader GetForCurrentView(string name) => GetOrCreateNamedResourceLoader(name);

	public static ResourceLoader GetForViewIndependentUse() => GetOrCreateNamedResourceLoader(DefaultResourceLoaderName);

	public static ResourceLoader GetForViewIndependentUse(string name) => GetOrCreateNamedResourceLoader(name);

	[NotImplemented]
	public string GetStringForUri(Uri uri) => throw new NotSupportedException();

	[NotImplemented]
	public static string GetStringForReference(Uri uri) => throw new NotSupportedException();
}
