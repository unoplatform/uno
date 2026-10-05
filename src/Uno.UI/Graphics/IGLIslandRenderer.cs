#nullable enable

namespace Uno.Graphics;

/// <summary>
/// Draws the content of an <see cref="IGLIsland"/>. Every call is made with the island's GL context current.
/// </summary>
public interface IGLIslandRenderer
{
	/// <summary>The island's GL context was created.</summary>
	void Init();

	/// <summary>Draws a frame into <paramref name="framebuffer"/>, which is already bound.</summary>
	void Render(uint framebuffer, int width, int height);

	/// <summary>The island's GL context is about to be destroyed.</summary>
	void Destroy();

	/// <summary>The island could not get a usable GL context; it will not render.</summary>
	void OnUnavailable();
}
