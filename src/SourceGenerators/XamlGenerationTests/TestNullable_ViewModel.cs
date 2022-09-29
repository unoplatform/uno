using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace UnoNullableContextGeneratedBug
{
    public class TestViewModel
    {
        public TestViewModel()
        {
            ObjectInstance = new ThrowawayClass();
        }

        public ThrowawayClass? ObjectInstance { get; set; } = new ThrowawayClass();
    }

    public class ThrowawayClass
    {
    }
}
