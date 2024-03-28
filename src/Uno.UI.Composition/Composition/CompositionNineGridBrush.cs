#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Composition
{
	public partial class CompositionNineGridBrush : CompositionBrush
	{
		private float _bottomInset;
		private float _bottomInsetScale = 1.0f;

		private float _leftInset;
		private float _leftInsetScale = 1.0f;

		private float _rightInset;
		private float _rightInsetScale = 1.0f;

		private float _topInset;
		private float _topInsetScale = 1.0f;

		private bool _isCenterHollow;
		private CompositionBrush? _source;

		internal CompositionNineGridBrush(Compositor compositor) : base(compositor)
		{

		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			// Call base implementation - Visual calls Compositor.InvalidateRender().
			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

			switch (propertyName)
			{
				case nameof(Source):
					OnSourceChangedPartial(Source);
					break;
				case nameof(IsCenterHollow):
					OnIsCenterHollowChangedPartial(IsCenterHollow);
					break;
				case nameof(BottomInset):
					OnBottomInsetChangedPartial(BottomInset);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(LeftInset):
					OnLeftInsetChangedPartial(LeftInset);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(RightInset):
					OnRightInsetChangedPartial(RightInset);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(TopInset):
					OnTopInsetChangedPartial(TopInset);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(BottomInsetScale):
					OnBottomInsetScaleChangedPartial(BottomInsetScale);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(LeftInsetScale):
					OnLeftInsetScaleChangedPartial(LeftInsetScale);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(RightInsetScale):
					OnRightInsetScaleChangedPartial(RightInsetScale);
					OnInsetOrScaleChangedPartial();
					break;
				case nameof(TopInsetScale):
					OnTopInsetScaleChangedPartial(TopInsetScale);
					OnInsetOrScaleChangedPartial();
					break;
				default:
					break;
			}
		}

		partial void OnSourceChangedPartial(CompositionBrush? source);
		partial void OnIsCenterHollowChangedPartial(bool isCenterHollow);

		partial void OnBottomInsetChangedPartial(float insest);
		partial void OnLeftInsetChangedPartial(float insest);
		partial void OnRightInsetChangedPartial(float insest);
		partial void OnTopInsetChangedPartial(float insest);

		partial void OnBottomInsetScaleChangedPartial(float scale);
		partial void OnLeftInsetScaleChangedPartial(float scale);
		partial void OnRightInsetScaleChangedPartial(float scale);
		partial void OnTopInsetScaleChangedPartial(float scale);

		partial void OnInsetOrScaleChangedPartial();

		public float BottomInset
		{
			get { return _bottomInset; }
			set { SetProperty(ref _bottomInset, value); }
		}

		public float BottomInsetScale
		{
			get { return _bottomInsetScale; }
			set { SetProperty(ref _bottomInsetScale, value); }
		}

		public float LeftInset
		{
			get { return _leftInset; }
			set { SetProperty(ref _leftInset, value); }
		}

		public float LeftInsetScale
		{
			get { return _leftInsetScale; }
			set { SetProperty(ref _leftInsetScale, value); }
		}

		public float RightInset
		{
			get { return _rightInset; }
			set { SetProperty(ref _rightInset, value); }
		}

		public float RightInsetScale
		{
			get { return _rightInsetScale; }
			set { SetProperty(ref _rightInsetScale, value); }
		}

		public float TopInset
		{
			get { return _topInset; }
			set { SetProperty(ref _topInset, value); }
		}

		public float TopInsetScale
		{
			get { return _topInsetScale; }
			set { SetProperty(ref _topInsetScale, value); }
		}

		public bool IsCenterHollow
		{
			get { return _isCenterHollow; }
			set { SetProperty(ref _isCenterHollow, value); }
		}

		public CompositionBrush? Source
		{
			get { return _source; }
			set { SetProperty(ref _source, value); }
		}

		public void SetInsets(float inset)
		{
			BottomInset = inset;
			LeftInset = inset;
			RightInset = inset;
			TopInset = inset;
		}

		public void SetInsets(float left, float top, float right, float bottom)
		{
			BottomInset = bottom;
			LeftInset = left;
			RightInset = right;
			TopInset = top;
		}

		public void SetInsetScales(float scale)
		{
			BottomInsetScale = scale;
			LeftInsetScale = scale;
			RightInsetScale = scale;
			TopInsetScale = scale;
		}

		public void SetInsetScales(float left, float top, float right, float bottom)
		{
			BottomInsetScale = bottom;
			LeftInsetScale = left;
			RightInsetScale = right;
			TopInsetScale = top;
		}
	}
}
