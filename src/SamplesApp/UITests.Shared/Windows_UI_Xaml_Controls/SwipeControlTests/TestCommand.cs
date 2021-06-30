// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeControl_TestUI/TestCommand.cs

using MUXControlsTestApp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwipeControl_TestUI
{
    public class TestCommand : System.Windows.Input.ICommand
    {
        SwipeControlPage page;
        public TestCommand(SwipeControlPage page)
        {
            this.page = page;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return (parameter as String).Equals("command's text");
        }

        public void Execute(object parameter)
        {
            page.ChangeText(parameter as String);

            if(CanExecuteChanged != null)
            {
                // do nothing
            }
        }
    }
}
