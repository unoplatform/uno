/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class When_ArrayOfStringParams
{
	/* Pack=4 */
	MyArray_Length : number;
	MyArray : Array<string>;
	public static unmarshal(pData:number) : When_ArrayOfStringParams
	{
		let ret = new When_ArrayOfStringParams();
		
		{
			ret.MyArray_Length = Number(Module.getValue(pData + 0, "i32"));
		}
		
		{
			var pArray = Module.getValue(pData + 4, "*");
			if(pArray !== 0)
			{
				ret.MyArray = new Array<string>();
				for(var i=0; i<ret.MyArray_Length; i++)
				{
					ret.MyArray.push(String(MonoRuntime.conv_string(Module.getValue(pArray + i*4, "*"))));
				}
			}
			else
			
			{
				ret.MyArray = null;
			}
		}
		return ret;
	}
}
