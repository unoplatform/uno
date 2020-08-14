/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_TryGetValueReturn
{
	/* Pack=4 */
	public Value : string;
	public HasValue : boolean;
	public marshal(pData:number)
	{
		
		{
			const stringLength = lengthBytesUTF8(this.Value);
			const pString = Module._malloc(stringLength + 1);
			stringToUTF8(this.Value, pString, stringLength + 1);
			Module.setValue(pData + 0, pString, "*");
		}
		Module.setValue(pData + 4, this.HasValue, "i32");
	}
}
