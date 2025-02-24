using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Media.Animation;

internal interface IKeyFramesProvider
{
	IEnumerable GetKeyFrames();
}
