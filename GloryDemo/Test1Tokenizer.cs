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
    using GloryDemo;
    using System.Collections.Generic;
    
    internal class Test1Tokenizer : TableTokenizer {
        internal static DfaEntry[] DfaTable = new DfaEntry[] {
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        43,
                                        43}, 1),
                            new DfaTransitionEntry(new int[] {
                                        100,
                                        100}, 2)}, -1),
                new DfaEntry(new DfaTransitionEntry[0], 2),
                new DfaEntry(new DfaTransitionEntry[0], 3)};
        internal static int[] NodeFlags = new int[] {
                0,
                0,
                0,
                0};
        internal static int[][] BlockEnds = new int[][] {
                null,
                null,
                null,
                null};
        public Test1Tokenizer(IEnumerable<char> input) : 
                base(Test1Tokenizer.DfaTable, Test1Tokenizer.BlockEnds, Test1Tokenizer.NodeFlags, input) {
        }
        public const int Implicit = 2;
        public const int Implicit2 = 3;
    }
}
