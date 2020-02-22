//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GloryDemo {
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Text;
    using GloryDemo;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.7.0.0")]
    internal struct DfaEntry {
        public DfaTransitionEntry[] Transitions;
        public int AcceptSymbolId;
        public DfaEntry(DfaTransitionEntry[] transitions, int acceptSymbolId) {
            this.Transitions = transitions;
            this.AcceptSymbolId = acceptSymbolId;
        }
    }
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.7.0.0")]
    internal struct DfaTransitionEntry {
        public int[] PackedRanges;
        public int Destination;
        public DfaTransitionEntry(int[] packedRanges, int destination) {
            this.PackedRanges = packedRanges;
            this.Destination = destination;
        }
    }
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.7.0.0")]
    internal class TableTokenizer : object, IEnumerable<Token> {
        public const int ErrorSymbol = -1;
        private DfaEntry[] _dfaTable;
        private int[][] _blockEnds;
        private int[] _nodeFlags;
        private IEnumerable<char> _input;
        public IEnumerator<Token> GetEnumerator() {
            return new TableTokenizerEnumerator(this._dfaTable, this._blockEnds, this._nodeFlags, this._input.GetEnumerator());
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
        public TableTokenizer(DfaEntry[] dfaTable, int[][] blockEnds, int[] nodeFlags, IEnumerable<char> input) {
            if ((null == dfaTable)) {
                throw new ArgumentNullException("dfaTable");
            }
            if ((null == blockEnds)) {
                throw new ArgumentNullException("blockEnds");
            }
            if ((null == nodeFlags)) {
                throw new ArgumentNullException("nodeFlags");
            }
            if ((null == input)) {
                throw new ArgumentNullException("input");
            }
            this._dfaTable = dfaTable;
            this._blockEnds = blockEnds;
            this._nodeFlags = nodeFlags;
            this._input = input;
        }
    }
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.7.0.0")]
    internal class TableTokenizerEnumerator : object, IEnumerator<Token> {
        public const int ErrorSymbol = -1;
        private const int _EosSymbol = -2;
        private const int _Disposed = -4;
        private const int _BeforeBegin = -3;
        private const int _AfterEnd = -2;
        private const int _InnerFinished = -1;
        private const int _Enumerating = 0;
        private const int _TabWidth = 4;
        private DfaEntry[] _dfaTable;
        private int[][] _blockEnds;
        private int[] _nodeFlags;
        private IEnumerator<char> _input;
        private int _inputCurrent;
        private int _state;
        private Token _current;
        private StringBuilder _buffer;
        private int _line;
        private int _column;
        private long _position;
        public TableTokenizerEnumerator(DfaEntry[] dfaTable, int[][] blockEnds, int[] nodeFlags, IEnumerator<char> input) {
            this._dfaTable = dfaTable;
            this._blockEnds = blockEnds;
            this._nodeFlags = nodeFlags;
            this._input = input;
            this._state = TableTokenizerEnumerator._BeforeBegin;
            this._buffer = new StringBuilder();
            this._line = 1;
            this._column = 1;
            this._position = 0;
        }
        public Token Current {
            get {
                if ((TableTokenizerEnumerator._Enumerating > this._state)) {
                    if ((TableTokenizerEnumerator._BeforeBegin == this._state)) {
                        throw new InvalidOperationException("The cursor is before the start of the enumeration");
                    }
                    if ((TableTokenizerEnumerator._AfterEnd == this._state)) {
                        throw new InvalidOperationException("The cursor is after the end of the enumeration");
                    }
                    if ((TableTokenizerEnumerator._Disposed == this._state)) {
                        TableTokenizerEnumerator._ThrowDisposed();
                    }
                }
                return this._current;
            }
        }
        object System.Collections.IEnumerator.Current {
            get {
                return this.Current;
            }
        }
        void System.Collections.IEnumerator.Reset() {
            if ((TableTokenizerEnumerator._Disposed == this._state)) {
                TableTokenizerEnumerator._ThrowDisposed();
            }
            if ((false 
                        == (TableTokenizerEnumerator._BeforeBegin == this._state))) {
                this._input.Reset();
            }
            this._state = TableTokenizerEnumerator._BeforeBegin;
            this._line = 1;
            this._column = 1;
            this._position = 0;
        }
        bool System.Collections.IEnumerator.MoveNext() {
            if ((TableTokenizerEnumerator._Enumerating > this._state)) {
                if ((TableTokenizerEnumerator._Disposed == this._state)) {
                    TableTokenizerEnumerator._ThrowDisposed();
                }
                if ((TableTokenizerEnumerator._AfterEnd == this._state)) {
                    return false;
                }
            }
            this._current = default(Token);
            this._current.Line = this._line;
            this._current.Column = this._column;
            this._current.Position = this._position;
            this._buffer.Clear();
            this._current.SymbolId = this._Lex();
            bool done = false;
            for (
            ; (false == done); 
            ) {
                done = true;
                if ((TableTokenizerEnumerator.ErrorSymbol < this._current.SymbolId)) {
                    int[] be = this._blockEnds[this._current.SymbolId];
                    if (((null != be) 
                                && (false 
                                == (0 == be.Length)))) {
                        if ((false == this._TryReadUntilBlockEnd(be))) {
                            this._current.SymbolId = TableTokenizerEnumerator.ErrorSymbol;
                        }
                    }
                    if (((TableTokenizerEnumerator.ErrorSymbol < this._current.SymbolId) 
                                && (false 
                                == (0 
                                == (this._nodeFlags[this._current.SymbolId] & 1))))) {
                        done = false;
                        this._current.Line = this._line;
                        this._current.Column = this._column;
                        this._current.Position = this._position;
                        this._buffer.Clear();
                        this._current.SymbolId = this._Lex();
                    }
                }
            }
            this._current.Value = this._buffer.ToString();
            if ((TableTokenizerEnumerator._EosSymbol == this._current.SymbolId)) {
                this._state = TableTokenizerEnumerator._AfterEnd;
            }
            return (false 
                        == (TableTokenizerEnumerator._AfterEnd == this._state));
        }
        void IDisposable.Dispose() {
            this._input.Dispose();
            this._state = TableTokenizerEnumerator._Disposed;
        }
        bool _MoveNextInput() {
            if (this._input.MoveNext()) {
                this._inputCurrent = System.Convert.ToInt32(this._input.Current);
                if (char.IsHighSurrogate(this._input.Current)) {
                    if ((false == this._input.MoveNext())) {
                        throw new IOException("Unexpected end of input while looking for Unicode low surrogate.");
                    }
                    this._position = (this._position + 1);
                    this._inputCurrent = char.ConvertToUtf32(System.Convert.ToChar(this._inputCurrent), this._input.Current);
                }
                if ((false 
                            == (TableTokenizerEnumerator._BeforeBegin == this._state))) {
                    this._position = (this._position + 1);
                    if ((10 == this._inputCurrent)) {
                        this._column = 1;
                        this._line = (this._line + 1);
                    }
                    else {
                        if ((9 == this._inputCurrent)) {
                            this._column = (this._column + TableTokenizerEnumerator._TabWidth);
                        }
                        else {
                            this._column = (this._column + 1);
                        }
                    }
                }
                else {
                    if ((10 == this._inputCurrent)) {
                        this._column = 1;
                        this._line = (this._line + 1);
                    }
                    else {
                        if ((9 == this._inputCurrent)) {
                            this._column = (TableTokenizerEnumerator._TabWidth - 1);
                        }
                        else {
                            this._column = (this._column - 1);
                        }
                    }
                }
                return true;
            }
            this._inputCurrent = -1;
            this._state = TableTokenizerEnumerator._InnerFinished;
            return false;
        }
        bool _TryReadUntil(int character) {
            int ch = this._inputCurrent;
            this._buffer.Append(char.ConvertFromUtf32(ch));
            if ((ch == character)) {
                return true;
            }
            for (
            ; (this._MoveNextInput() 
                        && (false 
                        == (this._inputCurrent == character))); 
            ) {
                this._buffer.Append(char.ConvertFromUtf32(this._inputCurrent));
            }
            if ((false 
                        == (this._state == TableTokenizerEnumerator._InnerFinished))) {
                this._buffer.Append(char.ConvertFromUtf32(this._inputCurrent));
                return (System.Convert.ToInt32(this._input.Current) == character);
            }
            return false;
        }
        bool _TryReadUntilBlockEnd(int[] blockEnd) {
            for (
            ; ((false 
                        == (TableTokenizerEnumerator._InnerFinished == this._state)) 
                        && this._TryReadUntil(blockEnd[0])); 
            ) {
                bool found = true;
                for (int i = 1; (found 
                            && (i < blockEnd.Length)); i = (i + 1)) {
                    if (((false == this._MoveNextInput()) 
                                || (false 
                                == (this._inputCurrent == blockEnd[i])))) {
                        found = false;
                    }
                    else {
                        if ((false 
                                    == (TableTokenizerEnumerator._InnerFinished == this._state))) {
                            this._buffer.Append(char.ConvertFromUtf32(this._inputCurrent));
                        }
                    }
                }
                if (found) {
                    this._MoveNextInput();
                    return true;
                }
            }
            return false;
        }
        int _Lex() {
            int acceptSymbolId;
            int dfaState = 0;
            if ((TableTokenizerEnumerator._BeforeBegin == this._state)) {
                if ((false == this._MoveNextInput())) {
                    acceptSymbolId = this._dfaTable[dfaState].AcceptSymbolId;
                    if ((false 
                                == (-1 == acceptSymbolId))) {
                        return acceptSymbolId;
                    }
                    else {
                        return TableTokenizerEnumerator.ErrorSymbol;
                    }
                }
                this._state = TableTokenizerEnumerator._Enumerating;
            }
            else {
                if (((TableTokenizerEnumerator._InnerFinished == this._state) 
                            || (TableTokenizerEnumerator._AfterEnd == this._state))) {
                    return TableTokenizerEnumerator._EosSymbol;
                }
            }
            bool done = false;
            for (
            ; (false == done); 
            ) {
                int nextDfaState = -1;
                for (int i = 0; (i < this._dfaTable[dfaState].Transitions.Length); i = (i + 1)) {
                    DfaTransitionEntry entry = this._dfaTable[dfaState].Transitions[i];
                    bool found = false;
                    for (int j = 0; (j < entry.PackedRanges.Length); j = (j + 1)) {
                        int ch = this._inputCurrent;
                        int first = entry.PackedRanges[j];
                        j = (j + 1);
                        int last = entry.PackedRanges[j];
                        if ((ch <= last)) {
                            if ((first <= ch)) {
                                found = true;
                            }
                            j = (int.MaxValue - 1);
                        }
                    }
                    if (found) {
                        nextDfaState = entry.Destination;
                        i = (int.MaxValue - 1);
                    }
                }
                if ((false 
                            == (-1 == nextDfaState))) {
                    this._buffer.Append(char.ConvertFromUtf32(this._inputCurrent));
                    dfaState = nextDfaState;
                    if ((false == this._MoveNextInput())) {
                        acceptSymbolId = this._dfaTable[dfaState].AcceptSymbolId;
                        if ((false 
                                    == (-1 == acceptSymbolId))) {
                            return acceptSymbolId;
                        }
                        else {
                            return TableTokenizerEnumerator.ErrorSymbol;
                        }
                    }
                }
                else {
                    done = true;
                }
            }
            acceptSymbolId = this._dfaTable[dfaState].AcceptSymbolId;
            if ((false 
                        == (-1 == acceptSymbolId))) {
                return acceptSymbolId;
            }
            else {
                this._buffer.Append(char.ConvertFromUtf32(this._inputCurrent));
                this._MoveNextInput();
                return TableTokenizerEnumerator.ErrorSymbol;
            }
        }
        static void _ThrowDisposed() {
            throw new ObjectDisposedException("TableTokenizerEnumerator");
        }
    }
    internal class JsonTokenizer : TableTokenizer {
        internal static DfaEntry[] DfaTable = new DfaEntry[] {
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        9,
                                        13,
                                        32,
                                        32}, 1),
                            new DfaTransitionEntry(new int[] {
                                        34,
                                        34}, 2),
                            new DfaTransitionEntry(new int[] {
                                        44,
                                        44}, 5),
                            new DfaTransitionEntry(new int[] {
                                        45,
                                        45}, 6),
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        48}, 7),
                            new DfaTransitionEntry(new int[] {
                                        49,
                                        57}, 13),
                            new DfaTransitionEntry(new int[] {
                                        58,
                                        58}, 14),
                            new DfaTransitionEntry(new int[] {
                                        91,
                                        91}, 15),
                            new DfaTransitionEntry(new int[] {
                                        93,
                                        93}, 16),
                            new DfaTransitionEntry(new int[] {
                                        102,
                                        102}, 17),
                            new DfaTransitionEntry(new int[] {
                                        110,
                                        110}, 22),
                            new DfaTransitionEntry(new int[] {
                                        116,
                                        116}, 26),
                            new DfaTransitionEntry(new int[] {
                                        123,
                                        123}, 30),
                            new DfaTransitionEntry(new int[] {
                                        125,
                                        125}, 31)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        9,
                                        13,
                                        32,
                                        32}, 1)}, 21),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        0,
                                        33,
                                        35,
                                        91,
                                        93,
                                        1114111}, 2),
                            new DfaTransitionEntry(new int[] {
                                        34,
                                        34}, 3),
                            new DfaTransitionEntry(new int[] {
                                        92,
                                        92}, 4)}, -1),
                new DfaEntry(new DfaTransitionEntry[0], 11),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        0,
                                        1114111}, 2)}, -1),
                new DfaEntry(new DfaTransitionEntry[0], 20),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        48}, 7),
                            new DfaTransitionEntry(new int[] {
                                        49,
                                        57}, 13)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        46,
                                        46}, 8),
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        57}, 9),
                            new DfaTransitionEntry(new int[] {
                                        69,
                                        69,
                                        101,
                                        101}, 10)}, 10),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        57}, 9)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        57}, 9),
                            new DfaTransitionEntry(new int[] {
                                        69,
                                        69,
                                        101,
                                        101}, 10)}, 10),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        43,
                                        43,
                                        45,
                                        45}, 11),
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        57}, 12)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        57}, 12)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        57}, 12)}, 10),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        46,
                                        46}, 8),
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        57}, 13),
                            new DfaTransitionEntry(new int[] {
                                        69,
                                        69,
                                        101,
                                        101}, 10)}, 10),
                new DfaEntry(new DfaTransitionEntry[0], 19),
                new DfaEntry(new DfaTransitionEntry[0], 15),
                new DfaEntry(new DfaTransitionEntry[0], 16),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        97,
                                        97}, 18)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        108,
                                        108}, 19)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        115,
                                        115}, 20)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        101,
                                        101}, 21)}, -1),
                new DfaEntry(new DfaTransitionEntry[0], 13),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        117,
                                        117}, 23)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        108,
                                        108}, 24)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        108,
                                        108}, 25)}, -1),
                new DfaEntry(new DfaTransitionEntry[0], 14),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        114,
                                        114}, 27)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        117,
                                        117}, 28)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        101,
                                        101}, 29)}, -1),
                new DfaEntry(new DfaTransitionEntry[0], 12),
                new DfaEntry(new DfaTransitionEntry[0], 17),
                new DfaEntry(new DfaTransitionEntry[0], 18)};
        internal static int[] NodeFlags = new int[] {
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                1};
        internal static int[][] BlockEnds = new int[][] {
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null};
        public JsonTokenizer(IEnumerable<char> input) : 
                base(JsonTokenizer.DfaTable, JsonTokenizer.BlockEnds, JsonTokenizer.NodeFlags, input) {
        }
        public const int number = 10;
        public const int @string = 11;
        public const int @true = 12;
        public const int @false = 13;
        public const int @null = 14;
        public const int lbracket = 15;
        public const int rbracket = 16;
        public const int lbrace = 17;
        public const int rbrace = 18;
        public const int colon = 19;
        public const int comma = 20;
        public const int whitespace = 21;
    }
}
