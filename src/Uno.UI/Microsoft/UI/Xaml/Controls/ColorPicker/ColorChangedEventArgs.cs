using Windows.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public class ColorChangedEventArgs : IColorChangedEventArgs
	{
		private Color m_oldColor;
		private Color m_newColor;

		public Color OldColor
		{
			get => m_oldColor;
			set => m_oldColor = value;
		}

		public Color NewColor
		{
			get => m_newColor;
			set => m_newColor = value;
		}
	}
}
