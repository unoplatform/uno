using System.ComponentModel;
using System.Security;
using CoreGraphics;

namespace Windows.Foundation;

public partial struct Size
{
	public static implicit operator Size(CGSize size) => new Size(size.Width, size.Height);

	public static implicit operator CGSize(Size size) => new CGSize(size.Width, size.Height);
}
