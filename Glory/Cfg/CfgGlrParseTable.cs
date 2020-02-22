﻿using System;
using System.Collections.Generic;

namespace Glory
{
#if CFGLIB
	public
#endif
	class CfgGlrParseTable : List<IDictionary<string, ICollection<(int RuleOrStateId, string Left, string[] Right)>>>
	{
		public int[][][][] ToArray(IEnumerable<string> symbolTable)
		{
			var nts = new List<string>();
			var ts = new List<string>();
			if (null == symbolTable)
			{
				var st = new List<string>();
				for (int ic = Count, i = 0; i < ic; ++i)
				{
					foreach (var kvp in this[i])
					{
						foreach (var item in kvp.Value)
						{
							if (!nts.Contains(item.Left))
								nts.Add(item.Left);
						}
					}
				}
				for (int ic = Count, i = 0; i < ic; ++i)
				{
					foreach (var kvp in this[i])
					{
						foreach (var item in kvp.Value)
						{
							for (var j = 0; j < item.Right.Length; j++)
							{
								var s = item.Right[j];
								if (!nts.Contains(s) && !ts.Contains(s))
									ts.Add(s);
							}
						}
					}
				}
				st.AddRange(nts);
				st.AddRange(ts);
				if (!st.Contains("#EOS"))
					st.Add("#EOS");
				if (!st.Contains("#ERROR"))
					st.Add("#ERROR");
				symbolTable = st;
			}
			var stbl = symbolTable as IList<string>;
			if (null == stbl)
				stbl = new List<string>(symbolTable);
			var result = new int[Count][][][];
			for (var i = 0; i < result.Length; i++)
			{
				var r = new int[stbl.Count - 1][][];
				result[i] = r;
				for (var j = 0; j < r.Length; j++)
				{
					r[j] = null;
					var d = this[i];
					ICollection<(int RuleOrStateId, string Left, string[] Right)> pcol;
					if (d.TryGetValue(stbl[j], out pcol))
					{

						var m = 0;
						var arr = new int[pcol.Count][];
						foreach (var e in pcol)
						{

							if (null != e.Left)
							{
								var ea = new int[2 + e.Right.Length];
								arr[m] = ea;
								ea[0] = e.RuleOrStateId;
								ea[1] = stbl.IndexOf(e.Left);
								for (var k = 2; k < ea.Length; k++)
									ea[k] = stbl.IndexOf(e.Right[k - 2]);
							}
							else
							{
								var ea = new int[1];
								arr[m] = ea;
								ea[0] = e.RuleOrStateId;
							}
							r[j] = arr;

							++m;
						}
					}
				}
			}
			return result;
		}
	}
}