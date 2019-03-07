using System.Numerics;
using Windows.Foundation;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Uno.Logging;

namespace Windows.UI.Xaml.Media
{
	public partial class CompositeTransform
	{
		/// <summary>
		/// Set the view of the inner transform
		/// </summary>
		protected override void OnAttachedToView()
		{
			base.OnAttachedToView();
			_innerTransform.View = View;
		}

		internal override Matrix3x2 ToNativeTransform(Size size)
		{
			return _innerTransform.ToNativeTransform(size);
		}
	}
}

