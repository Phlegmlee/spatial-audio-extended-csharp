using System.Collections.Generic;
using System.Linq;
namespace SpatialAudioCS;

public static class ListExt
{
	/// <summary>
	/// Resize a List&lt;T&gt; to desired size.
	/// </summary>
	/// <param name="list">List&lt;T&gt; to resize.</param>
	/// <param name="size">Desired new size.</param>
	/// <param name="element">Default value to insert.</param>
	public static void Resize<T>(this List<T> list, int size, T element = default(T))
	{
		int count = list.Count;

		if (size < count)
		{
			list.RemoveRange(size, count - size);
		}
		else if (size > count)
		{
			if (size > list.Capacity) list.Capacity = size;
			list.AddRange(Enumerable.Repeat(element, size - count));
		}
	}
}