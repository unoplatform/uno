#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Xml.Dom
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NodeType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Invalid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ElementNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AttributeNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TextNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DataSectionNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EntityReferenceNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EntityNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProcessingInstructionNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CommentNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DocumentNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DocumentTypeNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DocumentFragmentNode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotationNode,
		#endif
	}
	#endif
}
