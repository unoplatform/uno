// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SelectionModel.idl, commit 4b206bce3

using System.ComponentModel;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a data structure for managing selection state for an
/// <see cref="ItemsRepeater"/> or any data set that supports the
/// <see cref="ICustomPropertyProvider"/> interface.
/// </summary>
public partial class SelectionModel : INotifyPropertyChanged, ICustomPropertyProvider
{
}
