namespace Windows.ApplicationModel.DataTransfer
{

  // only one operation is supported - copy; 'none' is default value in DataPackage
	public   enum DataPackageOperation 
	{
		None,
		Copy,
		
		// two options without support
		[global::Uno.NotImplemented]
		Move,
		[global::Uno.NotImplemented]
		Link,
		
	}
}
