/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class When_ArrayOfIntParams
{
	/* Pack=1 */
	MyArray_Length : number;
	MyArray : Array<number>;
	public static unmarshal(pData:number) : When_ArrayOfIntParams
	{
		let ret = new When_ArrayOfIntParams();
		
		{
			ret.MyArray_Length = Number(Module.getValue(pData + 0, "i32"));
		}
		
		{
			var pArray = Module.getValue(pData + 4, "*");
			if(pArray !== 0)
			{
				ret.MyArray = new Array<number>();
				for(var i=0; i<ret.MyArray_Length; i++)
				{
					var value = Module.getValue(pArray + i * 4, "*");
					ret.MyArray.push(Number(value));
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
