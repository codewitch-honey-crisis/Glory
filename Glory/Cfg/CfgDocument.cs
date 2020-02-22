using LC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Glory
{
	internal static class CfgErrors
	{
		public const int NoStartSymbol = 250;
		public const int DuplicateRule = 251;
		public const int UnreferencedNonTerminal = 252;
		public const int DuplicateStartAttribute=253;
		public const int NoRules=254;
		public const int ShiftReduceConflict = 255;
		public const int ReduceReduceConflict = 256;
	}
	/// <summary>
	/// Represents a Context Free Grammar (CFG)
	/// </summary>
#if CFGLIB
	public
#endif
	partial class CfgDocument : CfgNode, IEquatable<CfgDocument>, ICloneable
	{
		HashSet<string> _ntCache = null;
		HashSet<string> _sCache = null;
		
		/// <summary>
		/// Clears any cached data
		/// </summary>
		public void ClearCache()
		{
			_ntCache = null;
			_sCache = null;
		}
		/// <summary>
		/// Caches the CFG information for fast access
		/// </summary>
		/// <remarks>Must be rebuilt or cleared after writing the document or it will be stale</remarks>
		public void RebuildCache()
		{
			_ntCache = new HashSet<string>(EnumNonTerminals());
			_sCache = new HashSet<string>(EnumSymbols());
		}
		/// <summary>
		/// Indicates the attributes for the symbols in the document
		/// </summary>
		public IDictionary<string, CfgAttributeList> AttributeSets { get; } = new ListDictionary<string, CfgAttributeList>();
		/// <summary>
		/// Indicates the rules in the document
		/// </summary>
		public List<CfgRule> Rules { get; } 
		/// <summary>
		/// Constructs a new instance of a document
		/// </summary>
		public CfgDocument()
		{
			Rules = new List<CfgRule>();
		}
		/// <summary>
		/// The start symbol. If not set, the first non-terminal is used.
		/// </summary>
		public string StartSymbol {
			get {
				foreach (var sattr in AttributeSets)
				{
					var i = sattr.Value.IndexOf("start");
					if (-1 < i && (sattr.Value[i].Value is bool) && (bool)sattr.Value[i].Value)
						return sattr.Key;
				}
				if (0 < Rules.Count)
					return Rules[0].Left;
				return null;
			}
			set {
				foreach (var sattr in AttributeSets)
				{
					var i = sattr.Value.IndexOf("start");
					if (-1 < i && (sattr.Value[i].Value is bool) && (bool)sattr.Value[i].Value)
						sattr.Value.RemoveAt(i);
				}
				if (null != value)
				{
					if (!IsSymbol(value))
						throw new KeyNotFoundException("The specified symbol does not exist");
					foreach (var sattrs in AttributeSets)
					{
						var i = sattrs.Value.IndexOf("start");
						if (-1 < i && (sattrs.Value[i].Value is bool) && (bool)sattrs.Value[i].Value)
							sattrs.Value.RemoveAt(i);
						if (sattrs.Key == value)
						{
							i = sattrs.Value.IndexOf("start");
							if (-1 < i)
								sattrs.Value[i].Value = true;
							else
								sattrs.Value.Add(new CfgAttribute("start", true));
						}
					}
				}
			}
		}
		/// <summary>
		/// Indicates whether the grammar is directly left recursive
		/// </summary>
		public bool IsDirectlyLeftRecursive {
			get {
				for (int ic = Rules.Count, i = 0; i < ic; ++i)
					if (Rules[i].IsDirectlyLeftRecursive)
						return true;
				return false;
			}
		}
		public bool IsLeftRecursive {
			get {
				CfgRule grulei;

				string gelj;
				var nils = this._GetNilNonTerminals();

				for (int ic=Rules.Count,i = 0; i < ic; ++i)
				{
					grulei = Rules[i];

					for (var j = 0; j < grulei.Right.Count; j++)
					{
						gelj = grulei.Right[j];

						if (!IsNonTerminal(gelj)) break;

						if (this._TestLeftRecusion(new List<string>(), gelj, nils))
							return true;

						if (!nils.Contains(gelj)) break;
					}
				}
				return false;
			}
		}
		List<string> _GetNilNonTerminals()
		{
			CfgRule grulei;
			string gelj;
			List<string> olds;
			var news = new List<string>();

			do
			{
				olds = news;
				news = new List<string>();

				for (int ic=Rules.Count,i = 0; i < ic; ++i)
				{
					grulei = Rules[i];

					// count rules with eps
					if (grulei.IsNil)
					{
						news.Add(grulei.Left);
						continue;
					}

					// count rules with all eps nonterminals
					for (var j = 0; j < grulei.Right.Count; j++)
					{
						gelj = grulei.Right[j];

						if (!IsNonTerminal(gelj))
							break;

						if (olds.Contains(gelj))
						{
							if (j != grulei.Right.Count - 1)
								continue;
							else
								news.Add(grulei.Left);
						}

						break;
					}
				}

			} while (olds.Count != news.Count);

			return news;
		}


		bool _TestLeftRecusion(IList<string> before, string current, IList<string> empty)
		{
			CfgRule grulei;
			string gelj;

			if (before.Contains(current))
			{
				return true;
			}

			before.Add(current);

			for (int ic=Rules.Count,i=0; i < ic; ++i)
			{
				grulei = Rules[i];

				if (grulei.Left != current) continue;

				for (var j = 0; j < grulei.Right.Count; j++)
				{
					gelj = grulei.Right[j];

					if (!IsNonTerminal(gelj)) break;

					if (this._TestLeftRecusion(before, gelj, empty))
						return true;

					if (!empty.Contains(gelj)) break;
				}
			}
			return false;
		}
		#region Symbols
		/// <summary>
		/// Lazy enumerates the non-terminals in the grammar
		/// </summary>
		/// <returns>A lazy enumeration of the non-terminals</returns>
		public IEnumerable<string> EnumNonTerminals()
		{
			var seen = new HashSet<string>();
			int ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
			{
				var s = Rules[i].Left;
				if (seen.Add(s))
					yield return s;
			}
			foreach (var attrs in AttributeSets)
			{
				if (seen.Add(attrs.Key))
				{
					var o = GetAttribute(attrs.Key, "virtual", false);
					if (o is bool && (bool)o)
					{
						yield return attrs.Key;
					}
					else
					{
						o = GetAttribute(attrs.Key, "abstract", false);
						if (o is bool && (bool)o)
						{
							yield return attrs.Key;
						}
						else
						{
							o = GetAttribute(attrs.Key, "dependency", false);
							if (o is bool && (bool)o)
								yield return attrs.Key;
							else
							{
								o = GetAttribute(attrs.Key, "terminal", null);
								if (null != o && o is bool && !(bool)o)
									yield return attrs.Key;
							}
						}
					}
				}
			}
		}
		/// <summary>
		/// Fills a list with all the non-terminals in the grammar in declared order
		/// </summary>
		/// <param name="result">The list to fill or null to fill a new list</param>
		/// <returns>The filled list</returns>
		public IList<string> FillNonTerminals(IList<string> result = null)
		{
			if (null == result)
				result = new List<string>();
			var ic = Rules.Count;
			
			for (var i = 0; i < ic; ++i)
			{
				var s = Rules[i].Left;
				if (!result.Contains(s))
					result.Add(s);
			}
			foreach (var attrs in AttributeSets)
			{
				if (!result.Contains(attrs.Key))
				{
					var o = GetAttribute(attrs.Key, "virtual", false);
					if (o is bool && (bool)o)
					{
						result.Add(attrs.Key);
					} else
					{
						o = GetAttribute(attrs.Key, "abstract", false);
						if (o is bool && (bool)o)
						{
							result.Add(attrs.Key);
						}
						else
						{
							o = GetAttribute(attrs.Key, "dependency", false);
							if (o is bool && (bool)o)
								result.Add(attrs.Key);
							else
							{
								o = GetAttribute(attrs.Key, "terminal", null);
								if (null != o && o is bool && !(bool)o)
									result.Add(attrs.Key);
							}
						}
					}
				}
			}
			return result;
		}
		/// <summary>
		/// Lazy enumerates all the symbols in the grammar
		/// </summary>
		/// <returns>A lazy enumeration of all the symbols in the grammar</returns>
		public IEnumerable<string> EnumSymbols()
		{
			foreach (var nt in EnumNonTerminals())
				yield return nt;
			foreach (var t in EnumTerminals())
				yield return t;
		}
		/// <summary>
		/// Fills a list with all the symbols in the grammar
		/// </summary>
		/// <param name="result">The list to fill with symbols or null to make a new one</param>
		/// <returns>The filled list</returns>
		public IList<string> FillSymbols(IList<string> result = null)
		{
			if (null == result)
				result = new List<string>();
			FillNonTerminals(result);
			FillTerminals(result);
			return result;
		}
		/// <summary>
		/// Lazy enumerates all terminals in the grammar
		/// </summary>
		/// <returns>A lazy enumeration of all terminals in the grammar</returns>
		public IEnumerable<string> EnumTerminals()
		{
			var seen = new HashSet<string>();
			seen.Add("#EOS");
			seen.Add("#ERROR");
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
				seen.Add(Rules[i].Left);
			
			for (var i = 0; i < ic; ++i)
			{
				var right = Rules[i].Right;
				for (int jc = right.Count, j = 0; j < jc; ++j)
				{
					var s = right[j];
					if (seen.Add(s))
						yield return s;
				}
			}
		
			foreach (var attrs in AttributeSets)
			{
				
				if (seen.Add(attrs.Key))
				{
					var o = GetAttribute(attrs.Key, "virtual", false);
					if (o is bool && (bool)o)
						continue;
					o = GetAttribute(attrs.Key, "abstract", false);
					if (o is bool && (bool)o)
						continue;
					o = GetAttribute(attrs.Key, "abstract", false);
					if (o is bool && (bool)o)
						continue;
					o = GetAttribute(attrs.Key, "dependency", false);
					if (o is bool && (bool)o)
						continue;
					o = GetAttribute(attrs.Key, "terminal", null);
					if (null != o && o is bool && !(bool)o)
						continue;
					yield return attrs.Key;
				}
			}
			yield return "#EOS";
			yield return "#ERROR";
		}
		/// <summary>
		/// Fills a list with all the terminals in the grammar
		/// </summary>
		/// <param name="result">The list to fill with terminals or null to create a new one</param>
		/// <returns>A filled list</returns>
		public IList<string> FillTerminals(IList<string> result = null)
		{
			if (null == result)
				result = new List<string>();
			var seen = new HashSet<string>();
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
				seen.Add(Rules[i].Left);
			seen.Add("#EOS");
			seen.Add("#ERROR");
			for (var i = 0; i < ic; ++i)
			{
				var right = Rules[i].Right;
				for (int jc = right.Count, j = 0; j < jc; ++j)
				{
					var s = right[j];
					if (seen.Add(s))
						if (!result.Contains(s))
							result.Add(s);
				}
			}
			foreach (var attrs in AttributeSets)
			{
				if (seen.Add(attrs.Key))
				{
					var o = GetAttribute(attrs.Key, "virtual", false);
					if (o is bool && (bool)o)
						continue;
					o = GetAttribute(attrs.Key, "abstract", false);
					if (o is bool && (bool)o)
						continue;
					o = GetAttribute(attrs.Key, "abstract", false);
					if (o is bool && (bool)o)
						continue;
					o = GetAttribute(attrs.Key, "dependency", false);
					if (o is bool && (bool)o)
						continue;
					o = GetAttribute(attrs.Key, "terminal", null);
					if (null != o && o is bool && !(bool)o)
						continue;
					if (!result.Contains(attrs.Key))
						result.Add(attrs.Key);
				}
			}
			if (!result.Contains("#EOS"))
				result.Add("#EOS");
			if (!result.Contains("#ERROR"))
				result.Add("#ERROR");
			return result;
		}
		/// <summary>
		/// Gets the id for the specified symbol or -1 if not found
		/// </summary>
		/// <param name="symbol">The symbol</param>
		/// <returns>The id</returns>
		public int GetIdOfSymbol(string symbol)
		{
			var i = 0;
			foreach (var sym in EnumSymbols())
			{
				if (sym == symbol)
					return i;
				++i;
			}
			return -1;
		}
		/// <summary>
		/// Gets the symbol for the specified id
		/// </summary>
		/// <param name="id">The id</param>
		/// <returns>The symbol or null if not found</returns>
		public string GetSymbolOfId(int id)
		{
			var i = 0;
			foreach (var sym in EnumSymbols())
			{
				if (id == i)
					return sym;
				++i;
			}
			return null;
		}
		#endregion

		#region First/Follows/Predict
		/// <summary>
		/// Fills the firsts without resolving the non-terminals to terminals. This can be useful for error reporting
		/// </summary>
		/// <param name="result">The result to fill, or null to make a new one</param>
		/// <returns>The filled result</returns>
		public IDictionary<string, ICollection<string>> FillFirstNonTerminals(IDictionary<string, ICollection<string>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<string>>();
			// first add the terminals to the result
			foreach (var t in EnumTerminals())
			{
				var l = new HashSet<string>();
				l.Add(t);
				result.Add(t, l);
			}
			// foreach nonterm that has a firsts attribute

			// now for each rule, find every first right hand side and add it to the rule's left non-terminal result
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				ICollection<string> col;
				if (!result.TryGetValue(rule.Left, out col))
				{
					col = new HashSet<string>();
					result.Add(rule.Left, col);
				}
				if (!rule.IsNil)
				{
					var e = rule.Right[0];
					if (!col.Contains(e))
						col.Add(e);
				}
				else
				{
					// when it's nil, we represent that with a null
					if (!col.Contains(null))
						col.Add(null);
				}
			}

			return result;
		}
		/// <summary>
		/// Computes the firsts table, which contains a collection of terminals associated with a non-terminal.
		/// The terminals represent the terminals that will first appear in the non-terminal.
		/// </summary>
		/// <param name="result">The predict table</param>
		/// <returns>The result</returns>
		public IDictionary<string, ICollection<string>> FillFirsts(IDictionary<string, ICollection<string>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<string>>();
			FillFirstNonTerminals(result);
			// finally, for each non-terminal N we still have in the firsts, resolve FIRSTS(N)
			var done = false;
			while (!done)
			{
				done = true;
				foreach (var kvp in result)
				{
					foreach (var item in new List<string>(kvp.Value))
					{
						if (IsNonTerminal(item))
						{
							done = false;
							kvp.Value.Remove(item);
							foreach (var f in result[item])
								kvp.Value.Add(f);
						}
					}
				}
			}

			return result;
		}
		public IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> FillPredictNonTerminal(IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<(CfgRule Rule, string Symbol)>>();
			// first add the terminals to the result
			foreach (var t in EnumTerminals())
			{
				var l = new List<(CfgRule Rule, string Symbol)>();
				l.Add((null, t));
				result.Add(t, l);
			}
			
			// now for each rule, find every first right hand side and add it to the rule's left non-terminal result
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				ICollection<(CfgRule Rule, string Symbol)> col;
				if (!result.TryGetValue(rule.Left, out col))
				{
					col = new HashSet<(CfgRule Rule, string Symbol)>();
					result.Add(rule.Left, col);
				}
				if (!rule.IsNil)
				{
					var e = (rule, rule.Right[0]);
					if (!col.Contains(e))
						col.Add(e);
				}
				else
				{
					// when it's nil, we represent that with a null
					(CfgRule Rule, string Symbol) e = (rule, null);
					if (!col.Contains(e))
						col.Add(e);
				}
			}
			return result;
		}
		/// <summary>
		/// Computes the predict table, which contains a collection of terminals and associated rules for each non-terminal.
		/// The terminals represent the terminals that will first appear in the non-terminal.
		/// </summary>
		/// <param name="result">The predict table</param>
		/// <returns>The result</returns>
		public IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> FillPredict(IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<(CfgRule Rule, string Symbol)>>();
			var predictNT = FillPredictNonTerminal();

			// finally, for each non-terminal N we still have in the firsts, resolve FIRSTS(N)
			foreach (var kvp in predictNT)
			{
				var col = new HashSet<(CfgRule Rule, string Symbol)>();
				foreach (var item in kvp.Value)
				{
					var res = new List<string>();
					_ResolvePredict(item.Symbol, res, predictNT, new HashSet<string>());
					foreach (var r in res)
						col.Add((item.Rule, r));

				}
				result.Add(kvp.Key, col);
			}
			return result;
		}

		void _ResolvePredict(string symbol, ICollection<string> result, IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> predictNT, HashSet<string> seen)
		{
			if (seen.Add(symbol))
			{
				if (null != symbol)
				{
					ICollection<(CfgRule Rule, string Symbol)> col;
					if (!predictNT.TryGetValue(symbol, out col))
						throw new ArgumentException(string.Format("The symbol {0} was not found int the grammar.", symbol), "symbol");
					foreach (var p in col)
					{
						if (!IsNonTerminal(p.Symbol))
						{
							if (!result.Contains(p.Symbol))
								result.Add(p.Symbol);
						}
						else
						{
							_ResolvePredict(p.Symbol, result, predictNT, seen);
						}
					}
				}
				else if (!result.Contains(null))
					result.Add(null);
			}
		}
		/// <summary>
		/// Fills a dictionary with a set of terminals that can follow the given non-terminals
		/// </summary>
		/// <param name="result">The dictionary to fill</param>
		/// <returns>The filled result</returns>
		public IDictionary<string, ICollection<string>> FillFollows(IDictionary<string, ICollection<string>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<string>>();

			var followsNT = new Dictionary<string, ICollection<string>>();
			// we'll need the predict table
			var predict = FillPredict();
			var ss = StartSymbol;
			for (int ic = Rules.Count, i = -1; i < ic; ++i)
			{
				// here we augment the grammar by inserting START' -> START #EOS as the first rule.
				var rule = (-1 < i) ? Rules[i] : new CfgRule(GetAugmentedStartId(ss), ss, "#EOS");
				ICollection<string> col;

				// traverse the rule looking for symbols that follow non-terminals
				if (!rule.IsNil)
				{
					var jc = rule.Right.Count;
					for (var j = 1; j < jc; ++j)
					{
						var r = rule.Right[j];
						var target = rule.Right[j - 1];
						if (IsNonTerminal(target))
						{
							if (!followsNT.TryGetValue(target, out col))
							{
								col = new HashSet<string>();
								followsNT.Add(target, col);
							}
							foreach (var f in predict[r])
							{
								if (null != f.Symbol)
								{
									if (!col.Contains(f.Symbol))
										col.Add(f.Symbol);
								}
								else
								{
									if (!col.Contains(f.Rule.Left))
										col.Add(f.Rule.Left);
								}
							}
						}
					}

					var rr = rule.Right[jc - 1];
					if (IsNonTerminal(rr))
					{
						if (!followsNT.TryGetValue(rr, out col))
						{
							col = new HashSet<string>();
							followsNT.Add(rr, col);
						}
						if (!col.Contains(rule.Left))
							col.Add(rule.Left);
					}
				}
				else // rule is nil
				{
					// what follows is the rule's left nonterminal itself
					if (!followsNT.TryGetValue(rule.Left, out col))
					{
						col = new HashSet<string>();
						followsNT.Add(rule.Left, col);
					}

					if (!col.Contains(rule.Left))
						col.Add(rule.Left);
				}
			}
			// below we look for any non-terminals in the follows result and replace them
			// with their follows, so for example if N appeared, N would be replaced with 
			// the result of FOLLOW(N)
			var l = FillNonTerminals();
			foreach (var nt in l)
			{
				ICollection<string> col = new HashSet<string>();
				var res = new List<string>();
				_ResolveFollows(nt, col, followsNT, new HashSet<string>());
				ICollection<string> col2;
				if (result.TryGetValue(nt, out col2))
				{
					foreach (var s in col)
						col2.Add(s);
				} else
					result.Add(nt, col);
			}
			return result;

		}
		public IDictionary<string, ICollection<string>> FillFollowsNonTerminals(IDictionary<string, ICollection<string>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<string>>();

			// we'll need the predict table
			var predict = FillPredictNonTerminal();
			var ss = StartSymbol;
			for (int ic = Rules.Count, i = -1; i < ic; ++i)
			{
				// here we augment the grammar by inserting START' -> START #EOS as the first rule.
				var rule = (-1 < i) ? Rules[i] : new CfgRule(GetAugmentedStartId(ss), ss, "#EOS");
				ICollection<string> col;

				// traverse the rule looking for symbols that follow non-terminals
				if (!rule.IsNil)
				{
					var jc = rule.Right.Count;
					for (var j = 1; j < jc; ++j)
					{
						var r = rule.Right[j];
						var target = rule.Right[j - 1];
						if (IsNonTerminal(target))
						{
							if (!result.TryGetValue(target, out col))
							{
								col = new HashSet<string>();
								result.Add(target, col);
							}
							foreach (var f in predict[r])
							{
								if (null != f.Symbol)
								{
									if (!col.Contains(f.Symbol))
										col.Add(f.Symbol);
								}
								else
								{
									if (!col.Contains(f.Rule.Left))
										col.Add(f.Rule.Left);
								}
							}
						}
					}

					var rr = rule.Right[jc - 1];
					if (IsNonTerminal(rr))
					{
						if (!result.TryGetValue(rr, out col))
						{
							col = new HashSet<string>();
							result.Add(rr, col);
						}
						if (!col.Contains(rule.Left))
							col.Add(rule.Left);
					}
				}
				else // rule is nil
				{
					// what follows is the rule's left nonterminal itself
					if (!result.TryGetValue(rule.Left, out col))
					{
						col = new HashSet<string>();
						result.Add(rule.Left, col);
					}

					if (!col.Contains(rule.Left))
						col.Add(rule.Left);
				}
			}
			
			return result;

		}
		void _ResolveFollows(string symbol, ICollection<string> result, IDictionary<string, ICollection<string>> followsNT, HashSet<string> seen)
		{
			if (seen.Add(symbol))
			{
				if (IsNonTerminal(symbol))
				{
					ICollection<string> col;
					if (followsNT.TryGetValue(symbol, out col))
					{
						foreach (var f in col)
						{
							if (!IsNonTerminal(f))
							{
								if (!result.Contains(f))
									result.Add(f);
							}
							else
								_ResolveFollows(f, result, followsNT, seen);

						}
					}
				}
			}
		}
		/// <summary>
		/// Returns a new start name suitable for augmenting a grammar
		/// </summary>
		/// <param name="s">The original start symbol</param>
		/// <returns>A new start symbol</returns>
		public string GetAugmentedStartId(string s)
		{
			var i = 2;
			var ss = string.Concat(s, "start");
			while (IsSymbol(ss))
			{
				ss = string.Concat(s, "start", i.ToString());
				++i;
			}
			return ss;
		}
		#endregion
		/// <summary>
		/// Fills a list with all the rules for a given non-terminal symbol
		/// </summary>
		/// <param name="symbol">The non-terminal symbol</param>
		/// <param name="result">The list to fill, or null to create a new one</param>
		/// <returns>The filled list</returns>
		public IList<CfgRule> FillNonTerminalRules(string symbol, IList<CfgRule> result = null)
		{
			if (null == result)
				result = new List<CfgRule>();
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (rule.Left == symbol)
					result.Add(rule);
			}
			return result;
		}
		/// <summary>
		/// Checks the document for validity
		/// </summary>
		public void Validate()
		{
			CfgException.ThrowIfErrors(TryValidate());
		}
		/// <summary>
		/// Checks the document for validity
		/// </summary>
		/// <returns>A list of validation messages, warnings and errors</returns>
		public IList<CfgMessage> TryValidate()
		{
			var result = new List<CfgMessage>();
			var hasSS = false;
			string ss=null;
			var ll = 0;
			foreach (var sattr in AttributeSets)
			{
				var i = sattr.Value.IndexOf("start");
				var a = sattr.Value[i];
				ll = a.Line;
				if (-1 < i && (a.Value is bool) && (bool)a.Value)
				{
					if (hasSS)
						result.Add(new CfgMessage(ErrorLevel.Warning, CfgErrors.DuplicateStartAttribute, "Duplicate start attribute specified on "+sattr.Key, a.Line,a.Column, a.Position, a.FileOrUrl));
					ss = sattr.Key;
					hasSS = true;
				}
			}
			if(0==Rules.Count)
			{
				result.Add(new CfgMessage(ErrorLevel.Warning, CfgErrors.NoRules, "No rules specified", ll+1, 1, 0L, FileOrUrl));
				return result;
			}
			if (!hasSS)
			{
				ss = StartSymbol;
				var l = 1;
				var c = 1;
				var p = 0L;
				var f = FileOrUrl;
				var s = "";
				if(0<Rules.Count)
				{
					var r = Rules[0];
					l = r.Line;
					c = r.Column;
					p = r.Position;
					f = r.FileOrUrl;
					s = " - " + ss + " will be used";
				}
				result.Add(new CfgMessage(ErrorLevel.Warning, CfgErrors.NoStartSymbol, "No start symbol specified" +s, l, c, p, f));
			}
			var seen = new HashSet<CfgRule>();
			for(int ic=Rules.Count,i=0;i<ic;++i)
			{
				var rule = Rules[i];
				if (!seen.Add(rule))
					result.Add(new CfgMessage(ErrorLevel.Error, CfgErrors.DuplicateRule, "Duplicate rule: "+rule.ToString(), rule.Line, rule.Column, rule.Position, rule.FileOrUrl));
			}
			var nts = FillNonTerminals();
			
			for(int ic=nts.Count,i=0;i<ic;++i)
			{
				var nt = nts[i];
				if (nt == ss) continue;
				var found = false;
				for(int jc=Rules.Count,j=0;j<jc;++j)
				{
					var rule = Rules[j];
					for(int kc=rule.Right.Count,k=0;k<kc;++k)
					{
						var sym = rule.Right[k];
						if(sym==nt)
						{
							found = true;
							break;
						}
					}
				}
				if(!found)
				{
					var ntr = FillNonTerminalRules(nt);
					var l = 0;
					var c = 0;
					var p = 0L;
					var f = FileOrUrl;
					if(0<ntr.Count)
					{
						var r = ntr[0];
						l = r.Line;
						c = r.Column;
						p = r.Position;
						f = r.FileOrUrl;
					}
					result.Add(new CfgMessage(ErrorLevel.Warning, CfgErrors.UnreferencedNonTerminal, "Unreferenced non-terminal symbol: " + nt, l, c, p, f));
				}
			}
			return result;
		}
		/// <summary>
		/// Gets a unique symbol name. The name is guaranteed not to appear in the grammar at the time of creation
		/// </summary>
		/// <param name="name">The name to base the unique name on</param>
		/// <returns>A unique symbol name</returns>
		public string GetUniqueSymbolName(string name)
		{
			var result = name;
			var i = 2;
			
			while(IsSymbol(result))
			{
				result = string.Concat(name, i.ToString());
				++i;
			}
			return result;
		}
		/// <summary>
		/// Fills a list with the set of all symbols reachable from the given symbol, including itself
		/// </summary>
		/// <param name="symbol">The symbol to analyze</param>
		/// <param name="result">The list to fill or null to create a new one</param>
		/// <returns>The filled list</returns>
		public IList<string> FillClosure(string symbol, IList<string> result = null)
		{
			if (null == result)
				result = new List<string>();
			else if (result.Contains(symbol))
				return result;
			var rules = FillNonTerminalRules(symbol);
			if (0 != rules.Count) // non-terminal
			{
				if (!result.Contains(symbol))
					result.Add(symbol);
				for (int ic = rules.Count, i = 0; i < ic; ++i)
				{
					var rule = rules[i];
					for (int jc = rule.Right.Count, j = 0; j < jc; ++j)
						FillClosure(rule.Right[j], result);
				}
			}
			else if (IsSymbol(symbol))
			{
				// make sure this is a terminal
				if (!result.Contains(symbol))
					result.Add(symbol);
			}
			return result;
		}
		/// <summary>
		/// Fills a list with all rules that reference a given symbol
		/// </summary>
		/// <param name="symbol">The symbol to analyze</param>
		/// <param name="result">A list to fill, or null to create a new one</param>
		/// <returns>The filled list</returns>
		public IList<CfgRule> FillReferencesTo(string symbol, IList<CfgRule> result = null)
		{
			if (null == result)
				result = new List<CfgRule>();
			for(int ic = Rules.Count,i=0;i<ic;++i)
			{
				var rule = Rules[i];
				for(int jc=rule.Right.Count,j=0;j<jc;++j)
				{
					var right = rule.Right[j];
					if (right == symbol)
						result.Add(rule);
				}
			}
			return result;
		}
		/// <summary>
		/// Gets an attribute for the given symbol
		/// </summary>
		/// <param name="symbol">The symbol</param>
		/// <param name="name">The attribute name</param>
		/// <param name="default">A value to return if the attribute could not be found</param>
		/// <returns>The attribute value, or <paramref name="default"/></returns>
		public object GetAttribute(string symbol, string name, object @default = null)
		{
			CfgAttributeList l;
			if (AttributeSets.TryGetValue(symbol, out l))
			{
				var i = l.IndexOf(name);
				if (-1 < i)
					return l[i].Value;
			}
			return @default;
		}
		/// <summary>
		/// Sets an attribute for the given symbol
		/// </summary>
		/// <param name="symbol">The symbol</param>
		/// <param name="name">The attribute name</param>
		/// <param name="value">The new value</param>
		public void SetAttribute(string symbol,string name,object value)
		{
			CfgAttributeList l;
			if(!AttributeSets.TryGetValue(symbol,out l))
			{
				l = new CfgAttributeList();
				AttributeSets.Add(symbol,l);
			}
			var ai = l.IndexOf(name);
			if(-1<ai)
			{
				l[ai].Value = value;
			} else
			{
				l.Add(new CfgAttribute(name, value));
			}
		}
		/// <summary>
		/// Indicates whether a given symbol is non-terminal
		/// </summary>
		/// <param name="symbol">The symbol to analyze</param>
		/// <returns>True if the symbol is a non-terminal, otherwise false</returns>
		public bool IsNonTerminal(string symbol)
		{
			if (null != _ntCache) return _ntCache.Contains(symbol);
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
				if (Rules[i].Left == symbol)
					return true;
			return false;
		}
		/// <summary>
		/// Indicates whether a given symbol is present in the grammar
		/// </summary>
		/// <param name="symbol">The symbol to analyze</param>
		/// <returns>True if the symbol was present in the grammar, otherwise false</returns>
		public bool IsSymbol(string symbol)
		{
			if (AttributeSets.ContainsKey(symbol))
				return true;
			if (null != _sCache) return _sCache.Contains(symbol);
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (rule.Left == symbol)
					return true;
				for (int jc = rule.Right.Count, j = 0; j < jc; ++j)
				{
					if (symbol == rule.Right[j])
						return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Provides a string representation of the grammar
		/// </summary>
		/// <returns>A string representing the grammar</returns>
		public string ToString(string fmt)
		{
			var sb = new StringBuilder();
			if ("y" == fmt)
			{
				sb.Append("%token");
				foreach(var t in FillTerminals())
				{
					if ("#ERROR" != t && "#EOS" != t)
					{
						sb.Append(" ");
						sb.Append(t);
					}
				}
				sb.AppendLine();
				sb.Append("%%");
				foreach(var nt in FillNonTerminals())
				{
					sb.AppendLine();
					sb.Append(nt);
					sb.Append(" :");
					var rules = FillNonTerminalRules(nt);
					var first = true;
					foreach(var rule in rules)
					{
						if (first)
							first = false;
						else
						{
							sb.AppendLine();
							sb.Append("\t|");
						}
						foreach(var right in rule.Right)
						{
							sb.Append(" ");
							sb.Append(right);
						}
						
					}
					sb.Append(";");
				}
			}
			else
			{

				var hasAttrs = false;
				foreach (var attrSet in AttributeSets)
				{
					hasAttrs = true;
					if (0 < attrSet.Value.Count)
					{
						sb.Append(string.Concat(attrSet.Key, ": "));
						var delim = "";
						for (int jc = attrSet.Value.Count, j = 0; j < jc; ++j)
						{
							sb.Append(string.Concat(delim, attrSet.Value[j].ToString()));
							delim = ", ";
						}
						sb.AppendLine();
					}
				}
				if (hasAttrs)
					sb.AppendLine();
				for (int ic = Rules.Count, i = 0; i < ic; ++i)
					sb.AppendLine(Rules[i].ToString());
			}
			return sb.ToString();
		}
		public override string ToString()
		{
			return ToString(null);
		}
		/// <summary>
		/// Creates a deep-copy of the grammar
		/// </summary>
		/// <returns>A new grammar that is equivelent to the existing grammar</returns>
		public CfgDocument Clone()
		{
			var result = new CfgDocument();
			foreach (var attrs in AttributeSets)
			{
				var d = new CfgAttributeList();
				result.AttributeSets.Add(attrs.Key, d);
				foreach (var attr in attrs.Value)
					d.Add(attr.Clone());
			}
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
				result.Rules.Add(Rules[i].Clone());
			return result;
		}
		object ICloneable.Clone()
			=> Clone();
		/// <summary>
		/// Parses a grammar from its string representation
		/// </summary>
		/// <param name="string">The string representation of the the grammar</param>
		/// <returns>A grammar parsed from the string</returns>
		public static CfgDocument Parse(IEnumerable<char> @string)
			=> Parse(LexContext.Create(@string));
		/// <summary>
		/// Reads a grammar from the specified reader
		/// </summary>
		/// <param name="reader">The reader to read from</param>
		/// <returns>A grammar read from the reader</returns>
		public static CfgDocument ReadFrom(TextReader reader)
			=> Parse(LexContext.CreateFrom(reader));
		/// <summary>
		/// Reads a grammar from the specified file
		/// </summary>
		/// <param name="filename">The filename</param>
		/// <returns>A grammar read from the file</returns>
		public static CfgDocument ReadFrom(string filename)
		{
			using (var sr = File.OpenText(filename))
			{
				var result = ReadFrom(sr);
				result.FileOrUrl=filename;
				return result;
			}
		}
		/// <summary>
		/// Reads a grammar from the specified URL
		/// </summary>
		/// <param name="url">The url to read from</param>
		/// <returns>A grammar read from the URL</returns>
		public static CfgDocument ReadFromUrl(string url)
		{
			using (var lc = LexContext.CreateFromUrl(url))
			{
				var result = Parse(lc);
				result.FileOrUrl = url;
				return result;
			}
		}
		/// <summary>
		/// Parses a grammar from the specified LexContext
		/// </summary>
		/// <param name="pc">The LexContext</param>
		/// <returns>A grammar parsed from the LexContext</returns>
		internal static CfgDocument Parse(LexContext pc)
		{
			var result = new CfgDocument();
			while (-1 != pc.Current)
			{
				var line = pc.Line;
				var column = pc.Column;
				var position = pc.Position;
				while ('\n' == pc.Current)
				{
					pc.Advance();
					CfgNode.SkipCommentsAndWhitespace(pc);
				}
				var id = CfgNode.ParseIdentifier(pc);
				if (string.IsNullOrEmpty(id))
				{
					pc.Advance();
					CfgNode.SkipCommentsAndWhitespace(pc);
					continue;
				}
				CfgNode.SkipCommentsAndWhitespace(pc);

				pc.Expecting(':', '-', '=');
				if (':' == pc.Current) // attribute set
				{
					pc.Advance();
					var d = new CfgAttributeList();
					while (-1 != pc.Current && '\n' != pc.Current)
					{
						var attr = CfgAttribute.Parse(pc);
						d.Add(attr);

						CfgNode.SkipCommentsAndWhitespace(pc);
						pc.Expecting('\n', ',', -1);
						if (',' == pc.Current)
							pc.Advance();
					}
					result.AttributeSets.Add(id, d);
					CfgNode.SkipCommentsAndWhitespace(pc);
				}
				else if ('-' == pc.Current)
				{
					pc.Advance();
					pc.Expecting('>');
					pc.Advance();
					CfgNode.SkipCommentsAndWhitespace(pc);
					string primId = id;
					if ('\n' == pc.Current || -1==pc.Current)
					{
						var rule = new CfgRule(primId);
						result.Rules.Add(rule);
					}
					else
					{
						while (-1 != pc.Current && '\n' != pc.Current)
						{
							var rule = new CfgRule(primId);
							rule.SetLocation(line, column, position, pc.FileOrUrl);
							while (-1 != pc.Current && '|' != pc.Current && '\n' != pc.Current)
							{
								id = CfgNode.ParseIdentifier(pc);
								rule.Right.Add(id);
								CfgNode.SkipCommentsAndWhitespace(pc);
							}
							result.Rules.Add(rule);
							if ('|' == pc.Current)
							{
								pc.Advance();
								CfgNode.SkipCommentsAndWhitespace(pc);
								if ('\n' == pc.Current || -1 == pc.Current)
								{
									rule = new CfgRule(primId);
									result.Rules.Add(rule);
								}
							}

						}
					}
				}
				else if ('=' == pc.Current)
				{
					pc.TrySkipUntil('\n', true);
				}
				if ('\n' == pc.Current)
					pc.Advance();
				CfgNode.SkipCommentsAndWhitespace(pc);

			}
			return result;
		}
		#region Value semantics
		public bool Equals(CfgDocument rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			else if (ReferenceEquals(rhs, null)) return false;
			if (AttributeSets.Count != rhs.AttributeSets.Count)
				return false;
			foreach (var attrs in AttributeSets)
			{
				CfgAttributeList d;
				if (!rhs.AttributeSets.TryGetValue(attrs.Key, out d))
				{
					if (d.Count != attrs.Value.Count)
						return false;
					foreach (var attr in attrs.Value)
					{
						var i = d.IndexOf(attr.Name);
						if (0 > i || !Equals(d[i].Value, attr.Value))
							return false;
					}
				}
			}
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			var lc = Rules.Count;
			var rc = rhs.Rules.Count;
			if (lc != rc) return false;
			for (var i = 0; i < lc; ++i)
				if (Rules[i] != rhs.Rules[i])
					return false;
			return true;
		}
		public override bool Equals(object rhs)
			=> Equals(rhs as CfgDocument);

		public override int GetHashCode()
		{
			var result = 0;
			foreach (var attrs in AttributeSets)
			{
				if (null != attrs.Key)
					result ^= attrs.Key.GetHashCode();
				foreach (var attr in attrs.Value)
				{
					if (null != attr.Name)
						result ^= attr.Name.GetHashCode();
					if (null != attr.Value)
						result ^= attr.Value.GetHashCode();
				}
			}
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
				if (null != Rules[i])
					result ^= Rules[i].GetHashCode();

			return result;
		}
		public static bool operator ==(CfgDocument lhs, CfgDocument rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(CfgDocument lhs, CfgDocument rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion
	}
}