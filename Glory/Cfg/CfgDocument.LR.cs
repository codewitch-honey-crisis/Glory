using System;
using System.Collections.Generic;
using System.Text;

namespace Glory
{
	public enum LRTableKind
	{
		Lalr1 = 1
	}
	partial class CfgDocument
	{
		_LRFA _ToLRFA(IProgress<CfgLRProgress> progress)
		{
			if (null != progress)
				progress.Report(new CfgLRProgress(CfgLRStatus.ComputingStates, 0));
			var moves = new Dictionary<KeyValuePair<_LR0ItemSet, string>, _LR0ItemSet>();
			// TODO: this takes a long time sometimes
			var map = new Dictionary<_LR0ItemSet, _LRFA>();
			// create an augmented grammar - add rule {start} -> [[StartId]] 
			var ss = StartSymbol;
			var start = new CfgRule(GetAugmentedStartId(ss), new string[] { ss });
			var cl = new _LR0ItemSet();
			cl.AddItem(new _LR0Item(start, 0));
			
			_FillLRClosureInPlace(progress, cl);
			var lrfa = new _LRFA();
			lrfa.Accept = cl;
			var items = cl.Items.Count;
			map.Add(cl, lrfa);
			var done = false;
			int oc;
			while (!done)
			{
				done = true;
				var arr = new _LR0ItemSet[map.Keys.Count];
				map.Keys.CopyTo(arr, 0);
				for (var i = 0; i < arr.Length; ++i)
				{
					var itemSet = arr[i];
					foreach (var item in itemSet.Items)
					{
						var next = item.Next;
						if (!item.IsEnd)
						{

							_LR0ItemSet n;
							var key = new KeyValuePair<_LR0ItemSet, string>(itemSet, next);
							if (!moves.TryGetValue(key, out n))
							{
								n = _FillLRMove(itemSet, next, progress);
								moves.Add(key, n);
							}
							if (!map.ContainsKey(n))
							{
								done = false;

								var npda = new _LRFA();
								npda.Accept = n;
								map.Add(n, npda);
								items += n.Items.Count;
								if (null != progress)
									progress.Report(new CfgLRProgress(CfgLRStatus.ComputingConfigurations, items));
							}
					
							map[itemSet].Transitions[next] = map[n];
						} 
				
					}
				}
				if (!done)
				{
					oc = map.Count;
					if (null != progress)
						progress.Report(new CfgLRProgress(CfgLRStatus.ComputingStates, oc));
				}
			}
			return lrfa;
		}
		_LR0ItemSet _FillLRMove(_LR0ItemSet itemSet, string input, IProgress<CfgLRProgress> progress, _LR0ItemSet result = null)
		{
			if (null == result)
				result = new _LR0ItemSet();
			int i = 0;
			foreach (var item in itemSet.Items)
			{
				if (null != progress)
					progress.Report(new CfgLRProgress(CfgLRStatus.ComputingMove, i));
				var next = item.Next;
				if (!item.IsEnd )
				{
					if (Equals(next, input))
					{
						var lri = new _LR0Item(item.Left,item.Right, item.RightIndex + 1);
						result.AddItem(lri);	
					}
				}
				++i;
			}
			_FillLRClosureInPlace(progress, result);
			return result;
		}
		

		static int _IndexOfItemSet(IEnumerable<_LR0ItemSet> sets, _LR0ItemSet set)
		{
			var i = 0;
			foreach (var lris in sets)
			{
				if (lris.Equals(set))
					return i;
				++i;
			}
			return -1;
		}
		void _FillLRClosureInPlace(IProgress<CfgLRProgress> progress, _LR0ItemSet result)
		{
			var done = false;
			while (!done)
			{
				done = true;
				var l = new _LR0Item[result.Items.Count];
				result.Items.CopyTo(l, 0);
				for (var i = 0; i < l.Length; i++)
				{
					if (null != progress)
						progress.Report(new CfgLRProgress(CfgLRStatus.ComputingClosure, i));
					var item = l[i];
					var next = item.Next;
					if (!item.IsEnd || item.IsNil)
					{
						if (IsNonTerminal(next))
						{
							for (int jc = Rules.Count, j = 0; j < jc; ++j)
							{
								var r = Rules[j];
								if (r.Left == next)
								{
									var lri = new _LR0Item(r, 0);
									if(result.AddItem(lri))
										done = false;				
								}
							}
						}
					}
				}
			}
		}
		public IList<CfgMessage> TryToLR1ParseTable(out CfgLR1ParseTable parseTable, LRTableKind kind=LRTableKind.Lalr1, IProgress<CfgLRProgress> progress=null)
		{
			var result = new List<CfgMessage>();
			var start = GetAugmentedStartId(StartSymbol);
			var lrfa = _ToLRFA(progress);
			var trnsCfg = _LRFAToLRExtendedGrammar( lrfa, progress);
			trnsCfg.RebuildCache();
			var closure = new List<_LRFA>();
			parseTable = new CfgLR1ParseTable();

			var itemSets = new List<_LR0ItemSet>();

			lrfa.FillClosure(closure);
			var i = 0;
			foreach (var p in closure)
			{

				itemSets.Add(p.Accept);
				parseTable.Add(new Dictionary<string, (int RuleOrStateId, string Left, string[] Right)>());
				++i;
			}
			i = 0;
			foreach (var p in closure)
			{
				foreach (var trn in p.Transitions)
				{
					var idx = closure.IndexOf(trn.Value);

					parseTable[i].Add(
						trn.Key,
						(idx, null, null)
						);	
				}
				foreach (var item in p.Accept.Items)
				{
					if (item.IsEnd && Equals(item.Left, start))
					{
						parseTable[i].Add(
							"#EOS",
							(-1, null, null));
					}
				}
				++i;
			}
			var follows = trnsCfg.FillFollows();
			// work on our reductions now
			// each rule has a follows set associated with it
			var map = new Dictionary<CfgRule, ICollection<string>>(_Lalr1MergeRuleComparer.Default);
			foreach (var rule in trnsCfg.Rules)
			{
				ICollection<string> f;
				if (!map.TryGetValue(rule, out f))
					map.Add(rule, follows[rule.Left]);
				else
					foreach (var o in follows[rule.Left])
						if (!f.Contains(o))
							f.Add(o);
			}
			var j = 0;
		
			foreach (var mapEntry in map)
			{
				if (null != progress)
					progress.Report(new CfgLRProgress(CfgLRStatus.ComputingReductions, j));
				var rule = mapEntry.Key;
				var lr = rule.IsNil?_LRExtendedSymbol.Parse(rule.Left):_LRExtendedSymbol.Parse(rule.Right[rule.Right.Count - 1]);
				var left = _LRExtendedSymbol.Parse(rule.Left).Id;
				var right = new List<string>();
				foreach (var s in rule.Right)
					right.Add(_LRExtendedSymbol.Parse(s).Id);
				var newRule = new CfgRule(left, right);
				if (newRule.IsNil)
				{
					lr.To = lr.From;
				}
				if (!Equals(left, start))
				{
					foreach (var f in mapEntry.Value)
					{
						// build the rule data
						var rr = new string[newRule.Right.Count];
						for (var ri = 0; ri < rr.Length; ri++)
							rr[ri] = newRule.Right[ri];

						var iid = _LRExtendedSymbol.Parse(f).Id;
						(int RuleOrStateId, string Left, string[] Right) tuple;
						var rid = Rules.IndexOf(newRule);
						var newTuple = (RuleOrStateId: rid, Left: newRule.Left, Right: rr);
						// this gets rid of duplicate entries which crop up in the table
						if (!parseTable[lr.To].TryGetValue(iid, out tuple))
						{
							parseTable[lr.To].Add(iid,
								newTuple);
						}
						else
						{
							if (null == tuple.Right)
							{
								var nr = Rules[rid];
								var msg = new CfgMessage(ErrorLevel.Warning, CfgErrors.ShiftReduceConflict, string.Format("Shift-Reduce conflict on rule {0}, token {1}", nr, iid), nr.Line, nr.Column, nr.Position, FileOrUrl);
								if (!result.Contains(msg))
									result.Add(msg);
							}
							else
							{
								if (rid != tuple.RuleOrStateId)
								{
									var nr = Rules[rid];
									var msg = new CfgMessage(ErrorLevel.Error, CfgErrors.ReduceReduceConflict, string.Format("Reduce-Reduce conflict on rule {0}, token {1}", nr, iid), nr.Line, nr.Column, nr.Position, FileOrUrl);
									if (!result.Contains(msg))
										result.Add(msg);
								}
							}
						}
					}
				}
				++j;
			}
			return result;
		}
		public IList<CfgMessage> TryToGlrParseTable(out CfgGlrParseTable parseTable, LRTableKind kind = LRTableKind.Lalr1, IProgress<CfgLRProgress> progress = null)
		{
			var result = new List<CfgMessage>();
			var start = GetAugmentedStartId(StartSymbol);
			var lrfa = _ToLRFA(progress);
			var trnsCfg = _LRFAToLRExtendedGrammar(lrfa, progress);
			trnsCfg.RebuildCache();
			var closure = new List<_LRFA>();
			parseTable = new CfgGlrParseTable();

			var itemSets = new List<_LR0ItemSet>();

			lrfa.FillClosure(closure);
			var i = 0;
			foreach (var p in closure)
			{

				itemSets.Add(p.Accept);
				parseTable.Add(new Dictionary<string, ICollection<(int RuleOrStateId, string Left, string[] Right)>>());
				++i;
			}
			i = 0;
			foreach (var p in closure)
			{
				foreach (var trn in p.Transitions)
				{
					var idx = closure.IndexOf(trn.Value);
					ICollection<(int RuleOrStateId, string Left, string[] Right)> pcol;
					if(!parseTable[i].TryGetValue(trn.Key,out pcol))
					{
						pcol = new List<(int RuleOrStateId, string Left, string[] Right)>();
						parseTable[i].Add(trn.Key, pcol);
					}
					pcol.Add(
						(idx, null, null)
						);
				}
				foreach (var item in p.Accept.Items)
				{
					if (item.IsEnd && Equals(item.Left, start))
					{
						ICollection<(int RuleOrStateId, string Left, string[] Right)> pcol;
						if (!parseTable[i].TryGetValue("#EOS", out pcol))
						{
							pcol = new List<(int RuleOrStateId, string Left, string[] Right)>();
							parseTable[i].Add("#EOS", pcol);
						}

						pcol.Add(
							(-1, null, null));
						//break;
					}
				}
				++i;
			}
			var follows = trnsCfg.FillFollows();
			// work on our reductions now
			// each rule has a follows set associated with it
			var map = new Dictionary<CfgRule, ICollection<string>>(_Lalr1MergeRuleComparer.Default);
			foreach (var rule in trnsCfg.Rules)
			{
				ICollection<string> f;
				if (!map.TryGetValue(rule, out f))
					map.Add(rule, follows[rule.Left]);
				else
					foreach (var o in follows[rule.Left])
						if (!f.Contains(o))
							f.Add(o);
			}
			var j = 0;

			foreach (var mapEntry in map)
			{
				if (null != progress)
					progress.Report(new CfgLRProgress(CfgLRStatus.ComputingReductions, j));
				var rule = mapEntry.Key;
				var lr = rule.IsNil ? _LRExtendedSymbol.Parse(rule.Left) : _LRExtendedSymbol.Parse(rule.Right[rule.Right.Count - 1]);
				var left = _LRExtendedSymbol.Parse(rule.Left).Id;
				var right = new List<string>();
				foreach (var s in rule.Right)
					right.Add(_LRExtendedSymbol.Parse(s).Id);
				var newRule = new CfgRule(left, right);
				if (newRule.IsNil)
				{
					lr.To = lr.From;
				}
				if (!Equals(left, start))
				{
					foreach (var f in mapEntry.Value)
					{
						// build the rule data
						var rr = new string[newRule.Right.Count];
						for (var ri = 0; ri < rr.Length; ri++)
							rr[ri] = newRule.Right[ri];

						var iid = _LRExtendedSymbol.Parse(f).Id;
						var rid = Rules.IndexOf(newRule);
						var newTuple = (RuleOrStateId: rid, Left: newRule.Left, Right: rr);
						// this handles duplicate entries which crop up in the table
						ICollection<(int RuleOrStateId, string Left, string[] Right)> pcol;
						if (!parseTable[lr.To].TryGetValue(iid, out pcol))
						{
							pcol = new List<(int RuleOrStateId, string Left, string[] Right)>();
							parseTable[lr.To].Add(iid, pcol);
						}
						var found = false;
						foreach (var t in pcol)
						{
							if (t.Left == newTuple.Left && t.RuleOrStateId == newTuple.RuleOrStateId)
							{
								if ((t.Right == null && newTuple.Right == null) ||
									t.Right?.Length == newTuple.Right?.Length)
								{
									if (t.Right != null)
									{
										found = true;
										for (var ii = 0; ii < t.Right.Length; ++ii)
										{
											if (t.Right[ii] != newTuple.Right[ii])
											{
												found = false;
												break;
											}
										}
									}
								}
							}
							if (found)
								break;
						}
						if (!found)
							pcol.Add(newTuple);
					}
				}
				++j;
			}
			return result;
		}

		CfgDocument _LRFAToLRExtendedGrammar( _LRFA lrfa, IProgress<CfgLRProgress> progress)
		{
			var result = new CfgDocument();
			var closure = new List<_LRFA>();
			var itemSets = new List<_LR0ItemSet>();
			lrfa.FillClosure(closure);
			foreach (var p in closure)
			{
				itemSets.Add(p.Accept);
			}

			_LRExtendedSymbol start = null;
			int j = 0;
			foreach (var p in closure)
			{
				if (null != progress)
					progress.Report(new CfgLRProgress(CfgLRStatus.CreatingLRExtendedGrammar, j));

				int si = itemSets.IndexOf(p.Accept);

				foreach (var item in p.Accept.Items)
				{
					if (0 == item.RightIndex)
					{
						var next = item.Next;
						
						if (!item.IsEnd || item.IsNil)
						{
							int dst = -1;
							_LRFA dsts;
							if (p.Transitions.ContainsKey(item.Left))
							{
								dsts = p.Transitions[item.Left];
								dst = itemSets.IndexOf( dsts.Accept);
							}

							_LRExtendedSymbol left = new _LRExtendedSymbol(si, item.Left, dst);
							if (null == start)
								start = left;
							var right = new List<string>();
							var pc = p;
							foreach (var sym in item.Right)
							{
								int s1 = itemSets.IndexOf(pc.Accept);
								var pt = pc.Transitions[sym];
								int s2 = itemSets.IndexOf(pt.Accept);
								_LRExtendedSymbol n = new _LRExtendedSymbol(s1, sym, s2);
								right.Add(n.ToString());
								pc = pt;
							}
							
							result.Rules.Add(new CfgRule(left.ToString(), right));
						}	
					}
				}
				++j;
			}
			result.StartSymbol = start.ToString();
			return result;
		}
		class _Lalr1MergeRuleComparer : IEqualityComparer<CfgRule>
		{
			public static readonly _Lalr1MergeRuleComparer Default = new _Lalr1MergeRuleComparer();
			public bool Equals(CfgRule x, CfgRule y)
			{
				if (ReferenceEquals(x, y))
					return true;
				if (!_SymEq(_LRExtendedSymbol.Parse(x.Left), _LRExtendedSymbol.Parse(y.Left)))
					return false;
				var c = x.Right.Count;
				if (y.Right.Count != c) return false;

				if (0 == c) return true;

				var ll = _LRExtendedSymbol.Parse(x.Right[c - 1]);
				var lr = _LRExtendedSymbol.Parse(y.Right[c - 1]);
				if (!_SymEq(ll, lr)) return false;
				if (ll.To != lr.To) return false;

				for (int i = 0; i < c - 1; ++i)
					if (!_SymEq(_LRExtendedSymbol.Parse(x.Right[i]), _LRExtendedSymbol.Parse(y.Right[i])))
						return false;
				return true;
			}
			static bool _SymEq(_LRExtendedSymbol x, _LRExtendedSymbol y)
			{
				if (ReferenceEquals(x, y)) return true;
				else if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
					return false;
				var lhs = x;
				if (ReferenceEquals(lhs, null)) return false;
				var rhs = y;
				if (ReferenceEquals(rhs, null)) return false;

				return Equals(lhs.Id, rhs.Id);
			}
			public int GetHashCode(CfgRule obj)
			{
				var lr = _LRExtendedSymbol.Parse(obj.Left);
				var result = lr.Id.GetHashCode();
				for(int ic = obj.Right.Count,i=0;i<ic;++i)
				{
					lr = _LRExtendedSymbol.Parse(obj.Right[i]);
					if (null != lr)
					{
						result ^= (null != lr.Id) ? lr.Id.GetHashCode() : 0;
					}
				}
				if (null != lr)
					result ^= lr.To;
				return result;
			}
		}
		class _LR1MergeRuleComparer : IEqualityComparer<CfgRule>
		{
			public static readonly _LR1MergeRuleComparer Default = new _LR1MergeRuleComparer();
			public bool Equals(CfgRule x, CfgRule y)
			{
				if (ReferenceEquals(x, y)) return true;
				return object.Equals(x, y);
			}
			public int GetHashCode(CfgRule obj)
			{
				var lr = _LRExtendedSymbol.Parse(obj.Left);
				var result = lr.GetHashCode();
				for (int ic = obj.Right.Count, i = 0; i < ic; ++i)
				{
					lr = _LRExtendedSymbol.Parse(obj.Right[i]);
					if (null != lr)
						result ^= lr.GetHashCode();
				}
				if (null != lr)
					result ^= lr.To;
				return result;
			}
		}
		private struct _LR0Item : IEquatable<_LR0Item>, ICloneable
		{
			int _hashCode;
			
			public _LR0Item(string left, string[] right, int rightIndex)
			{
				Left = left;
				_hashCode = left.GetHashCode();
				if (0 == right.Length)
				{
		
					Right = new string[] {  };
				}
				else
				{
					Right = new string[right.Length];
					for(var i = 0;i<right.Length;i++)
					{
						_hashCode ^= right[i].GetHashCode();
						Right[i] = right[i];
					}
				}
				_hashCode ^= rightIndex;
				RightIndex = rightIndex;
			}
			public _LR0Item(CfgRule rule, int rightIndex)
			{
				Left = rule.Left;
				_hashCode = Left.GetHashCode();
				if (!rule.IsNil)
				{
					Right = new string[rule.Right.Count];
					rule.Right.CopyTo(Right, 0);
					for (var i = 0; i < Right.Length; i++)
					{
						var right = rule.Right[i];
						_hashCode ^= right.GetHashCode();
						Right[i] = right;
					}
				}
				else
					Right = new string[] { };
				_hashCode ^= rightIndex;
				RightIndex = rightIndex;
			}
			public bool IsNil {
				get {
					return 0 == Right.Length;
				}
			}
			public string Left { get;}
			public string[] Right { get; }

			public string Next {
				get {
					if (!IsEnd)
						return Right[RightIndex];
					return null;
				}
			}
			public int RightIndex { get; }

			public bool IsEnd { get { return RightIndex == Right.Length; } }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.Append(Left ?? "");
				sb.Append(" ->");
				for (var i = 0; i < Right.Length; i++)
				{
					if (i == RightIndex)
						sb.Append(" .");
					else
						sb.Append(" ");
					sb.Append(Right[i]);
				}
				if (Right.Length == RightIndex)
					sb.Append(".");

				return sb.ToString();
			}
			public bool Equals(_LR0Item rhs)
			{
				if (_hashCode != rhs._hashCode)
					return false;
				if (RightIndex != rhs.RightIndex)
					return false;
				if (Right.Length != rhs.Right.Length)
					return false;
				for (var i = 0; i < Right.Length; i++)
					if (Right[i] != rhs.Right[i])
						return false;
				return true;
			}
			public override bool Equals(object obj)
			{
				if (obj is _LR0Item)
					return Equals((_LR0Item)obj);
				return false;
			}
			public override int GetHashCode()
			{
				return _hashCode;
			}
			public static bool operator ==(_LR0Item lhs, _LR0Item rhs)
			{
				return lhs.Equals(rhs);
			}
			public static bool operator !=(_LR0Item lhs, _LR0Item rhs)
			{
				return !lhs.Equals(rhs);
			}
			public _LR0Item Clone()
			{
				return new _LR0Item(Left, Right, RightIndex);
			}
			object ICloneable.Clone() => Clone();
		}
		private sealed class _LR0ItemSetRefComparer : IEqualityComparer<_LR0ItemSet>
		{
			public static readonly _LR0ItemSetRefComparer Default = new _LR0ItemSetRefComparer();
			public bool Equals(_LR0ItemSet x, _LR0ItemSet y)
			{
				return ReferenceEquals(x, y);
			}

			public int GetHashCode(_LR0ItemSet obj)
			{
				return obj.GetHashCode();
			}
		}
		/*
		private sealed class _LR0ItemSetComparer : IEqualityComparer<ICollection<_LR0Item>>, IEqualityComparer<ISet<_LR0Item>>
		{
			public static readonly _LR0ItemSetComparer Default = new _LR0ItemSetComparer();
			public bool Equals(ICollection<_LR0Item> x, ICollection<_LR0Item> y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (null == x || null == y) return false;
				if (x.Count != y.Count)
					return false;
				foreach (var xx in x)
					if (!y.Contains(xx))
						return false;
				return true;
			}
			public bool Equals(ISet<_LR0Item> x, ISet<_LR0Item> y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (null == x || null == y) return false;
				return x.SetEquals(y);
			}

			public int GetHashCode(ICollection<_LR0Item> obj)
			{
				var result = 0;
				if (null == obj) return result;
				foreach (var lri in obj)
					result ^= lri.GetHashCode();
				return result;
			}
			public int GetHashCode(ISet<_LR0Item> obj)
			{
				var result = 0;
				if (null == obj) return result;
				foreach (var lri in obj)
					result ^= lri.GetHashCode();
				return result;
			}
		}
		*/
		private sealed class _LRExtendedSymbol : IEquatable<_LRExtendedSymbol>
		{
			public int From;
			public string Id;
			public int To;
			public _LRExtendedSymbol(int from, string id, int to)
			{
				From = from;
				Id = id;
				To = to;
			}
			public static _LRExtendedSymbol Parse(string str)
			{
				var sa = str.Split('/');
				if (1 == sa.Length)
				{
					return new _LRExtendedSymbol(-1, str, -1);
				}
				return new _LRExtendedSymbol(int.Parse(sa[0]), string.Join("/", sa, 1, sa.Length - 2), int.Parse(sa[sa.Length - 1]));
			}
			public override int GetHashCode()
			{
				return From ^ Id.GetHashCode() ^ To;
			}
			public override string ToString()
			{
				return string.Concat(From, "/", Id, "/", To);
			}
			public bool Equals(_LRExtendedSymbol obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return From == obj.From && To == obj.To && Equals(Id, obj.Id);
			}
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(this, obj)) return true;
				var rhs = obj as _LRExtendedSymbol;
				if (ReferenceEquals(null, rhs)) return false;
				return From == rhs.From && To == rhs.To && Equals(Id, rhs.Id);
			}
			public static bool operator ==(_LRExtendedSymbol lhs, _LRExtendedSymbol rhs)
			{
				if (ReferenceEquals(lhs, rhs)) return true;
				if (ReferenceEquals(null, rhs) ||
					ReferenceEquals(null, lhs))
					return false;

				return lhs.From == rhs.From && lhs.To == rhs.To && Equals(lhs.Id, rhs.Id);
			}
			public static bool operator !=(_LRExtendedSymbol lhs, _LRExtendedSymbol rhs)
			{
				if (ReferenceEquals(lhs, rhs)) return false;
				if (ReferenceEquals(null, rhs) ||
					ReferenceEquals(null, lhs))
					return true;

				return !(lhs.From == rhs.From && lhs.To == rhs.To && Equals(lhs.Id, rhs.Id));
			}
		}
		private sealed class _LRFA
		{
			public _LR0ItemSet Accept = null;
			public readonly Dictionary<string, _LRFA> Transitions = new Dictionary<string, _LRFA>();
			public ICollection<_LRFA> FillClosure(ICollection<_LRFA> result = null)
			{
				if (null == result)
					result = new HashSet<_LRFA>();
				if (result.Contains(this))
					return result;
				result.Add(this);
				foreach (var trns in Transitions)
					trns.Value.FillClosure(result);
				return result;
			}
		}
		// used to just use a container like ICollection<_LR0Item> but too slow
		private sealed class _LR0ItemSet : IEquatable<_LR0ItemSet>
		{
			int _hashCode;
			public readonly HashSet<_LR0Item> Items; // do not modify this list

			public _LR0ItemSet()
			{
				_hashCode = 0;
				Items = new HashSet<_LR0Item>();
			}
			public bool AddItem(_LR0Item item)
			{

				if (Items.Add(item))
				{
					_hashCode ^= item.GetHashCode();
					return true;
				}
				return false;
			}
			public override int GetHashCode()
			{
				return _hashCode;
			}
			public override bool Equals(object obj)
			{
				return Equals(obj as _LR0ItemSet);
			}
			public bool Equals(_LR0ItemSet rhs)
			{
				if (ReferenceEquals(this, rhs))
					return true;
				if (ReferenceEquals(rhs, null))
					return false;
				if (_hashCode != rhs._hashCode)
					return false;
				var ic = Items.Count;
				if (ic != rhs.Items.Count)
				{
					return false;
				}
				if(!Items.SetEquals(rhs.Items))
				{ 
					return false;
				}
		
				return true;
			}
		}
	}
}
