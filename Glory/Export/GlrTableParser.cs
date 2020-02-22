using System;
using System.Collections.Generic;

namespace Glory
{
	class GlrTableParser
	{
		private int[][][][] _parseTable;
		private List<GlrWorker> _workers;
		private GlrWorker _worker;
		private int _workerIndex;
		private LookAheadEnumerator _tokenEnum;
		private string[] _symbolTable;
		private ParseAttribute[][] _attributes;
		private int[] _errorSentinels;
		internal int NextWorkerId;
		private int _eosId;
		private int _errorId;
		private int _maxErrorCount;
		public GlrTableParser(int[][][][] parseTable, string[] symbolTable,ParseAttribute[][] attributes, int[] errorSentinels,IEnumerable<Token> tokenizer) :this(parseTable,symbolTable,attributes,errorSentinels,tokenizer,int.MaxValue)
		{

		}
		public GlrTableParser(int[][][][] parseTable,string[] symbolTable, ParseAttribute[][] attributes,int[] errorSentinels,  IEnumerable<Token> tokenizer,int maxErrorCount)
		{
			_parseTable = parseTable;
			_symbolTable = symbolTable;

			_attributes = attributes;
			_errorSentinels = errorSentinels;
			_eosId = Array.IndexOf(symbolTable, "#EOS");
			if (0 > _eosId)
				throw new ArgumentException("Error in symbol table", "symbolTable");
			_errorId = Array.IndexOf(symbolTable, "#ERROR");
			if (0 > _errorId)
				throw new ArgumentException("Error in symbol table", "symbolTable");
			_tokenEnum = new LookAheadEnumerator(tokenizer.GetEnumerator());
			_maxErrorCount = maxErrorCount;
			NextWorkerId = 1;
			_workerIndex = 0;
			_workers = new List<GlrWorker>(8);
			if (_tokenEnum.MoveNext())
			{
				_workers.Add(new GlrWorker(this, NextWorkerId, _parseTable, _errorId, _eosId,_errorSentinels, _workers, _tokenEnum));
				++NextWorkerId;
			}
		}
		
		public int TreeId { get { return _worker.Id; } }
		public LRNodeType NodeType {
			get {
				if (_worker.HasErrors)
					return LRNodeType.Error;
				return _worker.NodeType;
			}
		}
		public int Line {
			get {
				return _worker.CurrentToken.Line;
			}
		}
		public int Column {
			get {
				return _worker.CurrentToken.Column;
			}
		}
		public long Position {
			get {
				return _worker.CurrentToken.Position;
			}
		}
		public int SymbolId {
			get {
				var n = NodeType;
				if (LRNodeType.Shift==n)
					return _worker.CurrentToken.SymbolId;
				if (LRNodeType.Reduce== n)
					return _worker.RuleDefinition[0];
				if (LRNodeType.Error == n)
					return _errorId;
				if (LRNodeType.EndDocument == n)
					return _eosId;
				return -1;
				
			}
		}
		public ParseAttribute[] Attributes { 
			get {
				var sid = SymbolId;
				if(-1<sid && _attributes.Length>sid)
				{
					return _attributes[sid];
				}
				return null;
			}
		}

		public string[] RuleDefinition {
			get {
				if (LRNodeType.Reduce != _worker.NodeType)
					return null;
				var result = new string[_worker.RuleDefinition.Length];
				for (var i = 0; i < result.Length; ++i)
					result[i] = _symbolTable[_worker.RuleDefinition[i]];
				return result;
			}
		}
		public string Rule {
			get {
				var def = RuleDefinition;
				if (null == def) return null;
				var result = string.Concat(def[0], " ->");
				for (var i = 1; i < def.Length; ++i)
					result += string.Concat(" ", def[i]);
				return result;
			}
		}
		public string Symbol {
			get {
				var sid = SymbolId;
				if (0 > sid)
				{
					return null;
				}
				return _symbolTable[sid];
			}
		}
		public string Value {
			get {
				return _worker.Value;
			}
		}
		
		public bool Read()
		{
			if (0 == _workers.Count)
				return false;
			_workerIndex = (_workerIndex + 1) % _workers.Count;
			_worker = _workers[_workerIndex];
			while(!_worker.Read())
			{
				_workers.RemoveAt(_workerIndex);
				if (_workerIndex == _workers.Count)
					_workerIndex = 0;
				if (0 == _workers.Count)
					return false;
				_worker = _workers[_workerIndex];
			}
			var min = int.MaxValue;
			var ic = _workers.Count;
			for(var i=0;i<ic;++i)
			{
				GlrWorker w = _workers[i];
				if(0<i && w.ErrorCount>_maxErrorCount)
				{
					_workers.RemoveAt(i);
					--i;
					--ic;
				}
				if(min>w.Index)
				{
					min=w.Index;
				}
				if (0 == min)
					i = ic; // break
			}
			var j = min;
			while(j>0)
			{
				_tokenEnum.MoveNext();
				--j;
			}
			for (j = 0; j < ic; ++j)
			{
				GlrWorker w = _workers[j];
				w.Index -= min;
			}
			
			return true;
		}
		public ParseNode[] ParseReductions()
		{
			return ParseReductions(false, true,false);
		}
		public ParseNode[] ParseReductions(bool trim, bool transform, bool returnAll)
		{
			var map = new Dictionary<int, Stack<ParseNode>>();
			var oldId = 0;
			Dictionary<int,int> accepted = new Dictionary<int, int>();
			while (Read())
			{
				Stack<ParseNode> rs;
				// if this a new TreeId we haven't seen
				if (!map.TryGetValue(TreeId, out rs))
				{
					// if it's not the first id
					if (0 != oldId)
					{
						// clone the stack
						var l = new List<ParseNode>(map[oldId]);
						l.Reverse();
						rs = new Stack<ParseNode>(l);
					}
					else // otherwise create a new stack
						rs = new Stack<ParseNode>();
					// add the tree id to the map
					map.Add(TreeId, rs);
				}
				ParseNode p;
				var n = NodeType;


				if (LRNodeType.Shift == n)
				{
					p = new ParseNode(SymbolId,Symbol,null,Value,Attributes,Line,Column,Position);
					rs.Push(p);
				}
				else if (LRNodeType.Reduce == n)
				{
					if (!trim || 2 != RuleDefinition.Length)
					{
						var cl = new List<ParseNode>();
						for (var i = 1; RuleDefinition.Length > i; ++i)
						{
							if (0 != rs.Count)
							{
								ParseNode pc = rs.Pop();
								_AddChildren(pc, transform, cl);
								string s = pc.Symbol;
								if ("#ERROR" == s)
									i = RuleDefinition.Length; // break;
							}
							else
							{
								ParseNode pc = new ParseNode(_errorId, "#ERROR", null, "", Attributes, Line, Column, Position);
								_AddChildren(pc, transform, cl);
							}
						}
						p = new ParseNode(SymbolId, Symbol, cl.ToArray(), null, Attributes, Line, Column, Position);
						
						rs.Push(p);
					}
				}
				else if (LRNodeType.Error == n)
				{
					p = new ParseNode(_errorId, "#ERROR", null, Value, Attributes, Line, Column, Position);
					rs.Push(p);
				} else if(LRNodeType.Accept==n)
				{
					accepted.Add(TreeId,0);
				}
				
				oldId = TreeId;
			}
			List<ParseNode> result = new List<ParseNode>(map.Count);
			IEnumerator<KeyValuePair<int,Stack<ParseNode>>> e=map.GetEnumerator();
			var first = true;
			while(e.MoveNext())
			{
				KeyValuePair<int, Stack<ParseNode>> kvp = e.Current;
				if ((first || returnAll) || accepted.ContainsKey(kvp.Key))
				{
					Stack<ParseNode> rs = kvp.Value;
					if (0 != rs.Count)
					{
						ParseNode n = rs.Pop();
						List<ParseNode> cl = new List<ParseNode>();
						if (n.IsNonTerminal)
							cl.AddRange(n.Children);
						string s = n.Symbol;
						while ("#ERROR" != s && 0 < rs.Count)
							_AddChildren(rs.Pop(), transform, cl);
						n = new ParseNode(n.SymbolId, n.Symbol, cl.ToArray(), n.Value, n.Attributes, n.Line, n.Column, n.Position);
						result.Add(n);
					}
				}
				first = false;
			}
			
			return result.ToArray();
		}
		public string[] Symbols { get { return _symbolTable; } }
		void _AddChildren(ParseNode pc, bool transform, IList<ParseNode> result)
		{
			if (!transform)
			{
				result.Insert(0, pc);
				return;
			}
			if (pc.IsCollapsed)
			{
				if (null == pc.Value)
				{
					var ic = pc.Children.Length;
					for (var i = ic - 1; 0 <= i; --i)
						_AddChildren(pc.Children[i], transform, result);
				}
			}
			else
				result.Insert(0, pc);
		}
	}
}
