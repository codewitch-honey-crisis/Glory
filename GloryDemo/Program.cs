using System;

namespace GloryDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			string input;
			//using (var sr = new StreamReader(@"..\..\data2.json"))
			//	input = sr.ReadToEnd();
			input = "d+d+d+d";
			var test1Tokenizer = new Test1Tokenizer(input);
			var test1Parser = new Test1Parser(test1Tokenizer);
			
			foreach (var pt in test1Parser.ParseReductions(false, true,false))
			{
				Console.WriteLine(pt.ToString("t"));
				Console.WriteLine();
			}
			input = "1+3*-5";
			var expressionTokenizer = new ExpressionTokenizer(input);
			var expressionParser = new ExpressionParser(expressionTokenizer);
			foreach (var pt in expressionParser.ParseReductions())
			{
				Console.WriteLine(pt.ToString("t"));
				Console.WriteLine();
				Console.WriteLine(ExpressionParser.Evaluate(pt));
			}
			Console.WriteLine();
			input = "1+5-3+2";
			var test2Tokenizer = new Test2Tokenizer(input);
			var test2Parser = new Test2Parser(test2Tokenizer);
			foreach (var pt in test2Parser.ParseReductions())
			{
				if (!pt.HasErrors)
				{
					Console.WriteLine(pt.ToString("t"));
					Console.WriteLine();
					Console.WriteLine(Test2Parser.Evaluate(pt));
				}
			}
			input = "(int)foo.bar * baz";
			var seTokenizer = new SlangExpressionTokenizer(input);
			var seParser = new SlangExpressionParser(seTokenizer);
			foreach (var pt in seParser.ParseReductions())
			{
				if (!pt.HasErrors)
				{
					Console.WriteLine(pt.ToString("t"));
					Console.WriteLine();
				}
			}
		}
	}
}
