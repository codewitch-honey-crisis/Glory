using System.Collections.Generic;

namespace Glory
{
	/// <summary>
	/// Represents a list of attributes
	/// </summary>
#if CFGLIB
	public
#endif
		class CfgAttributeList : List<CfgAttribute>
	{
		/// <summary>
		/// Gets or sets an attribute by name
		/// </summary>
		/// <param name="name">The name of the attribute</param>
		/// <returns>The value of the attribute</returns>
		public object this[string name] {
			get {
				for (int ic = Count, i = 0; i < ic; ++i)
				{
					var item = this[i];
					if (name == item.Name)
						return item.Value;
				}
				throw new KeyNotFoundException();
			}
		}
		/// <summary>
		/// Returns the index of an attribute by name or a negative value if not found
		/// </summary>
		/// <param name="name">The attribute name</param>
		/// <returns></returns>
		public int IndexOf(string name)
		{
			for (int ic = Count, i = 0; i < ic; ++i)
			{
				var item = this[i];
				if (name == item.Name)
					return i;
			}
			return -1;
		}
		/// <summary>
		/// Indicates whether an attribute with the specified name exists in the list
		/// </summary>
		/// <param name="name">The attribute name</param>
		/// <returns>True if an attribute with the specified name is present, otherwise fakse</returns>
		public bool Contains(string name)
			=> -1 < IndexOf(name);
		/// <summary>
		/// Removes an attribute from the list
		/// </summary>
		/// <param name="name">The name of the attribute</param>
		/// <returns>True if the attribute was removed, or false if not present</returns>
		public bool Remove(string name)
		{
			var i = IndexOf(name);
			if (-1 < i)
			{
				RemoveAt(i);
				return true;
			}
			return false;
		}
	}
}
