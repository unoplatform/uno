#nullable enable

namespace Microsoft.UI.Composition
{
	public partial class InsetClip : CompositionClip
	{
		private float _topInset;
		private float _rightInset;
		private float _leftInset;
		private float _bottomInset;

		internal InsetClip(Compositor compositor) : base(compositor)
		{

		}

		public float TopInset
		{
			get => _topInset;
			set => SetProperty(ref _topInset, value);
		}

		public float RightInset
		{
			get => _rightInset;
			set => SetProperty(ref _rightInset, value);
		}

		public float LeftInset
		{
			get => _leftInset;
			set => SetProperty(ref _leftInset, value);
		}

		public float BottomInset
		{
			get => _bottomInset;
			set => SetProperty(ref _bottomInset, value);
		}
	}
}
