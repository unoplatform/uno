// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System.Collections;
using System.Collections.Generic;
using DirectUI;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class CommandBarElementCollection : IObservableVector<ICommandBarElement>, IVector<ICommandBarElement>
	{
		bool m_notifyCollectionChanging;
		CommandBar? m_parent;
	}
}
