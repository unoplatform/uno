/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class GenericReturn
{
	/* Pack=1 */
	Value : string;
	public marshal(pData:number)
	{
		
		{
			var stringLength = lengthBytesUTF8(this.Value);
			var pString = Module._malloc(stringLength + 1);
			stringToUTF8(this.Value, pString, stringLength + 1);
			Module.setValue(pData + 0, pString, "*");
		}
	}
}
