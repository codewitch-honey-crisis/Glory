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
    
    internal class Test2Tokenizer : TableTokenizer {
        internal static DfaEntry[] DfaTable = new DfaEntry[] {
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        43,
                                        43}, 1),
                            new DfaTransitionEntry(new int[] {
                                        45,
                                        45}, 2),
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        57}, 3)}, -1),
                new DfaEntry(new DfaTransitionEntry[0], 2),
                new DfaEntry(new DfaTransitionEntry[0], 3),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new int[] {
                                        48,
                                        57}, 3)}, 1)};
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
        public Test2Tokenizer(IEnumerable<char> input) : 
                base(Test2Tokenizer.DfaTable, Test2Tokenizer.BlockEnds, Test2Tokenizer.NodeFlags, input) {
        }
        public const int integer = 1;
        public const int add = 2;
        public const int sub = 3;
    }
}
