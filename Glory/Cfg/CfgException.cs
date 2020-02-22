using System;
using System.Collections.Generic;
using System.Text;

namespace Glory
{
	/// <summary>
	/// Indicates a compound exception raised by a complex CFG operation
	/// </summary>
	[Serializable]
#if CFGLIB
	public
#endif
		sealed class CfgException : Exception
	{
		/// <summary>
		/// The messages
		/// </summary>
		public IList<CfgMessage> Messages { get; }
		/// <summary>
		/// Constructs an exception with the specified parameters
		/// </summary>
		/// <param name="message">The primary/first message</param>
		/// <param name="errorCode">The primary/first error code or -1</param>
		/// <param name="line">The associated 1 based line, if any</param>
		/// <param name="column">The associated 1 based column, if any</param>
		/// <param name="position">The associated 0 based position, if any</param>
		/// <param name="fileOrUrl">The associated file or URL, if any</param>
		public CfgException(string message, int errorCode = -1, int line = 0, int column = 0, long position = -1,string fileOrUrl=null) :
			this(new CfgMessage[] { new CfgMessage(ErrorLevel.Error, errorCode, message, line, column, position,fileOrUrl) })
		{ }
		/// <summary>
		/// Constructs an exception with the specified parameters
		/// </summary>
		/// <param name="messages">The messages</param>
		public CfgException(IEnumerable<CfgMessage> messages) : base(_FindFirstErrorMessage(messages))
		{
			Messages = new List<CfgMessage>(messages);
		}
		
		/// <summary>
		/// Throws if one or more of the messages is an error
		/// </summary>
		/// <param name="messages">The messages to examine</param>
		public static void ThrowIfErrors(IEnumerable<CfgMessage> messages)
		{
			if (null == messages) return;
			foreach (var m in messages)
				if (ErrorLevel.Error == m.ErrorLevel)
					throw new CfgException(messages);
		}
		static string _FindFirstErrorMessage(IEnumerable<CfgMessage> messages)
		{
			var l = new List<CfgMessage>(messages);
			if (null == messages) return "";
			int c = 0;
			foreach (var m in l)
			{
				if (ErrorLevel.Error == m.ErrorLevel)
				{
					if (1 == l.Count)
						return m.ToString();
					return string.Concat(m, " (multiple messages)");
				}
				++c;
			}
			foreach (var m in messages)
				return m.ToString();
			return "";
		}
	}
}
