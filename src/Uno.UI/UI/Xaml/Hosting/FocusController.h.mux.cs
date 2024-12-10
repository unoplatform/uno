// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\UIBridgeFocus\inc\FocusController.h, tag winui3/release/1.5.1, commit 3d10001ba8e12336cad392846b1030ba691b784a
#nullable enable

using Microsoft.UI.Input;
using Windows.UI.Xaml.Hosting;
using Uno.Disposables;
using Windows.Foundation;

namespace DirectUI;

partial class FocusController
{
	private readonly SerialDisposable _focusDepartingEvent = new();
	//private CoreComponentFocusable? _coreComponentFocusable;
	private InputFocusController? _inputObjectFocusable;

	private readonly SerialDisposable _gotFocusToken = new();

	private XamlSourceFocusNavigationRequest? _currentRequest;
}
