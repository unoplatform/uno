#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RichEditTextDocument : global::Windows.UI.Text.ITextDocument
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint UndoLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint RichEditTextDocument.UndoLimit is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "uint RichEditTextDocument.UndoLimit");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float DefaultTabStop
		{
			get
			{
				throw new global::System.NotImplementedException("The member float RichEditTextDocument.DefaultTabStop is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "float RichEditTextDocument.DefaultTabStop");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Text.CaretType CaretType
		{
			get
			{
				throw new global::System.NotImplementedException("The member CaretType RichEditTextDocument.CaretType is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "CaretType RichEditTextDocument.CaretType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Text.ITextSelection Selection
		{
			get
			{
				throw new global::System.NotImplementedException("The member ITextSelection RichEditTextDocument.Selection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IgnoreTrailingCharacterSpacing
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RichEditTextDocument.IgnoreTrailingCharacterSpacing is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "bool RichEditTextDocument.IgnoreTrailingCharacterSpacing");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool AlignmentIncludesTrailingWhitespace
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RichEditTextDocument.AlignmentIncludesTrailingWhitespace is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "bool RichEditTextDocument.AlignmentIncludesTrailingWhitespace");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.AlignmentIncludesTrailingWhitespace.get
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.AlignmentIncludesTrailingWhitespace.set
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.IgnoreTrailingCharacterSpacing.get
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.IgnoreTrailingCharacterSpacing.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void ClearUndoRedoHistory()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "void RichEditTextDocument.ClearUndoRedoHistory()");
		}
		#endif
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.CaretType.get
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.CaretType.set
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.DefaultTabStop.get
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.DefaultTabStop.set
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.Selection.get
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.UndoLimit.get
		// Forced skipping of method Windows.UI.Text.RichEditTextDocument.UndoLimit.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool CanCopy()
		{
			throw new global::System.NotImplementedException("The member bool RichEditTextDocument.CanCopy() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool CanPaste()
		{
			throw new global::System.NotImplementedException("The member bool RichEditTextDocument.CanPaste() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool CanRedo()
		{
			throw new global::System.NotImplementedException("The member bool RichEditTextDocument.CanRedo() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool CanUndo()
		{
			throw new global::System.NotImplementedException("The member bool RichEditTextDocument.CanUndo() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  int ApplyDisplayUpdates()
		{
			throw new global::System.NotImplementedException("The member int RichEditTextDocument.ApplyDisplayUpdates() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  int BatchDisplayUpdates()
		{
			throw new global::System.NotImplementedException("The member int RichEditTextDocument.BatchDisplayUpdates() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void BeginUndoGroup()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "void RichEditTextDocument.BeginUndoGroup()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void EndUndoGroup()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "void RichEditTextDocument.EndUndoGroup()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Text.ITextCharacterFormat GetDefaultCharacterFormat()
		{
			throw new global::System.NotImplementedException("The member ITextCharacterFormat RichEditTextDocument.GetDefaultCharacterFormat() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Text.ITextParagraphFormat GetDefaultParagraphFormat()
		{
			throw new global::System.NotImplementedException("The member ITextParagraphFormat RichEditTextDocument.GetDefaultParagraphFormat() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Text.ITextRange GetRange( int startPosition,  int endPosition)
		{
			throw new global::System.NotImplementedException("The member ITextRange RichEditTextDocument.GetRange(int startPosition, int endPosition) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Text.ITextRange GetRangeFromPoint( global::Windows.Foundation.Point point,  global::Windows.UI.Text.PointOptions options)
		{
			throw new global::System.NotImplementedException("The member ITextRange RichEditTextDocument.GetRangeFromPoint(Point point, PointOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void GetText( global::Windows.UI.Text.TextGetOptions options, out string value)
		{
			throw new global::System.NotImplementedException("The member void RichEditTextDocument.GetText(TextGetOptions options, out string value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void LoadFromStream( global::Windows.UI.Text.TextSetOptions options,  global::Windows.Storage.Streams.IRandomAccessStream value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "void RichEditTextDocument.LoadFromStream(TextSetOptions options, IRandomAccessStream value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Redo()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "void RichEditTextDocument.Redo()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void SaveToStream( global::Windows.UI.Text.TextGetOptions options,  global::Windows.Storage.Streams.IRandomAccessStream value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "void RichEditTextDocument.SaveToStream(TextGetOptions options, IRandomAccessStream value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void SetDefaultCharacterFormat( global::Windows.UI.Text.ITextCharacterFormat value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "void RichEditTextDocument.SetDefaultCharacterFormat(ITextCharacterFormat value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void SetDefaultParagraphFormat( global::Windows.UI.Text.ITextParagraphFormat value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "void RichEditTextDocument.SetDefaultParagraphFormat(ITextParagraphFormat value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void SetText( global::Windows.UI.Text.TextSetOptions options,  string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "void RichEditTextDocument.SetText(TextSetOptions options, string value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Undo()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.RichEditTextDocument", "void RichEditTextDocument.Undo()");
		}
		#endif
		// Processing: Windows.UI.Text.ITextDocument
	}
}
