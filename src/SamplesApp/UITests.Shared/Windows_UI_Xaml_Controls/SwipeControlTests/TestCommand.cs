// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
