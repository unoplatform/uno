using System;
using System.Linq;
using System.Linq.Expressions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation
{
	[Sample("Animations", "Transform", Description = _description)]
	public sealed partial class DoubleAnimation_TransformGroup : Page
	{
		private const string _description = @"This (automated) test validates animations of a TranformGroup.";

		private TimeSpan _duration;

		public DoubleAnimation_TransformGroup()
		{
			this.InitializeComponent();

#if DEBUG
			_duration = TimeSpan.FromSeconds(2);
#else
			_duration = TimeSpan.FromMilliseconds(400);
#endif
		}

		private void StartAnimations(object sender, RoutedEventArgs e)
		{
			var anims = new[]
			{
				CreateAnimation(TranslateHost, GetPath<TranslateTransform>(t => t.Y), 0, 50),
				CreateAnimation(ScaleHost, GetPath<ScaleTransform>(t => t.ScaleY), 1, 2),
				CreateAnimation(RotateHost, GetPath<RotateTransform>(t => t.Angle), 0, 90),
				CreateAnimation(SkewHost, GetPath<SkewTransform>(t => t.AngleY), 0, 45),
				CreateAnimation(CompositeHost, GetPath<CompositeTransform>(t => t.TranslateY), 0, 50),
				CreateAnimation(CrazyHost,
					("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)", 0, 50),
					("(UIElement.RenderTransform).(TransformGroup.Children)[5].(TransformGroup.Children)[2].(RotateTransform.Angle)", 0, 45)
				),
			}.ToList();

			Status.Text = "Animating";
			var running = anims.Count;
			anims.ForEach(a =>
			{
				a.Completed += (s, _) =>
				{
					if (--running == 0)
					{
						Status.Text = "Completed";
					}
				};
				a.Begin();
			});
		}

		private static string GetPath<TTransform>(Expression<Func<TTransform, object>> property)
			=> $"(UIElement.RenderTransform).(TransformGroup.Children)[0].({typeof(TTransform).Name}.{((property.Body as UnaryExpression)?.Operand as MemberExpression)?.Member.Name})";

		private Storyboard CreateAnimation(UIElement target, string path, double from, double to)
			=> CreateAnimation(target, (path, from, to));
		private Storyboard CreateAnimation(UIElement target, params (string path, double from, double to)[] properties)
		{
			var storyboard = new Storyboard();
			foreach (var property in properties)
			{
				var animation = new DoubleAnimation
				{
					From = property.from,
					To = property.to,
					Duration = _duration,
				};

				Storyboard.SetTarget(animation, target);
				Storyboard.SetTargetProperty(animation, property.path);

				storyboard.Children.Add(animation);
			}

			return storyboard;
		}
	}
}
