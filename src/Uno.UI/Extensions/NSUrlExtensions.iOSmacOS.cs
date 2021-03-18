using Foundation;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;


namespace Uno.UI.Extensions
{
    public static class NSUrlExtensions
    {
        public static Uri ToUri(this NSUrl nsUrl)
        {
            if(!nsUrl.AbsoluteString.HasValue())
            {
                return null;
            }
            return new Uri(nsUrl.AbsoluteString);
        }
    }
}
