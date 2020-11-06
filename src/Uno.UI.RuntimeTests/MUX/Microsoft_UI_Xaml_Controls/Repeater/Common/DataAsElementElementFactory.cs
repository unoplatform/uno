// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
    using Microsoft.UI.Xaml.Controls;
    using ElementFactory = Microsoft.UI.Xaml.Controls.ElementFactory;

    class DataAsElementElementFactory : ElementFactory
    {
        protected override UIElement GetElementCore(ElementFactoryGetArgs args)
        {
            return args.Data as UIElement;
        }

        protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
        {
        }
    }
}
