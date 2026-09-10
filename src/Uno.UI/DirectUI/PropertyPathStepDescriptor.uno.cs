#nullable enable

namespace DirectUI;

readonly partial struct PropertyPathStepDescriptor
{
	// CLR value copies and managed payloads replace the native move/reset and string-ownership operations.
	private PropertyPathStepDescriptor(PropertyPathStepDescriptorKind kind, object? payload, int index)
	{
		Kind = kind;
		_payload = payload;
		_index = index;
	}
}
