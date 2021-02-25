namespace Uno.Storage.Streams
{
	internal interface INativeStreamAdapter
	{
		// TODO: The adapter will not be an actual stream - it will be just a wrapper over native stream and offer
		// read/write functionality. On itself, however, it will not have any concept of Position.
		// Actual "Stream" instances will be returned by Rent() and will allow all the fun things with Position, SetLength, etc.
		// will be lifted to the actual Streams and those all will maintain it individually.
		// Length must be shared across all renteres - so that will be part of the adapter.
		void Rent();
	}
}
