using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		public Color BackgroundColor
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		private partial void OnLoadedPartial() { }

		private partial void OnUnloadedPartial() { }
		private void UpdateZoomedContentAlignment() { }

		private bool ChangeViewNative(double? horizontalOffset, double? verticalOffset, float? zoomFactor, bool disableAnimation)
		{
			HorizontalOffset = horizontalOffset ?? HorizontalOffset;
			VerticalOffset = verticalOffset ?? VerticalOffset;

			return true;
		}
	}
}
