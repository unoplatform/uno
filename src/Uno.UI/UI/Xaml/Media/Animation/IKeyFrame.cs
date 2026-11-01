#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>The part of WinUI's CKeyFrame the animation engine uses, implemented by every key frame type.</summary>
internal interface IKeyFrame
{
	KeyTime KeyTime { get; }

	/// <summary>The key time as a fraction of one iteration, resolved when the animation begins.</summary>
	float Percent { get; set; }

	bool IsDiscrete { get; }

	float GetEffectiveProgress(float progress);

	object? GetValue();
}
