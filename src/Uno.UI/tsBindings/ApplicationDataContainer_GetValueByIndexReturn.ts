/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_GetValueByIndexReturn
{
	/* Pack=1 */
	public Value : string;
	public marshal(pData:number)
	{
		
		{
			const stringLength = lengthBytesUTF8(this.Value);
			const pString = Module._malloc(stringLength + 1);
			stringToUTF8(this.Value, pString, stringLength + 1);
			Module.setValue(pData + 0, pString, "*");
		}
	}
}
