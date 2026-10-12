#nullable enable

namespace Uno.Graphics;

/// <summary>
/// Draws the content of an <see cref="IGLIsland"/>. <see cref="Init"/>, <see cref="Render"/> and <see cref="Destroy"/>
/// are called with the island's GL context current; <see cref="OnUnavailable"/> may be called without one.
/// </summary>
public interface IGLIslandRenderer
{
	/// <summary>The island's GL context was created.</summary>
	void Init();

	/// <summary>Draws a frame into <paramref name="framebuffer"/>, which is already bound.</summary>
	void Render(uint framebuffer, int width, int height);

	/// <summary>The island's GL context is about to be destroyed.</summary>
	void Destroy();

	/// <summary>The island could not get a usable GL context; it will not render. Must not make GL calls.</summary>
	void OnUnavailable();
}
