using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Media.Animation;

internal interface IKeyFramesProvider
{
	IEnumerable GetKeyFrames();
}
