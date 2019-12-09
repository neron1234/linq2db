﻿using System.Diagnostics;

namespace Tests.Playground
{
	public static class KDH
	{
		public static KDH<TKey, TData> Create<TKey, TData>(TKey key, TData data)
		{
			return new KDH<TKey, TData>(key, data);
		}
	}

	[DebuggerDisplay("Key: {Key}, Data: {Data}")]
	public class KDH<TKey, TData>
	{
		public KDH(TKey key, TData data)
		{
			Key = key;
			Data = data;
		}

		public TKey  Key { get; set; }
		public TData Data { get; set; }
	}

}
