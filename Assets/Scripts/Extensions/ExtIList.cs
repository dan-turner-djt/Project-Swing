using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtIList
{
	public static void AddRange<T>(this IList<T> list, IList<T> items, int startIndex, int endIndex)
	{
		for(int i = startIndex; i < items.Count && i < endIndex; i++)
		{
			list.Add(items[i]);	
		}
	}
}
