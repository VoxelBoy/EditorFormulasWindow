using System;

namespace EditorFormulas
{
	public class FormulaAttribute : Attribute
	{
		public string name;
		public string tooltip;
		public string author;


		public FormulaAttribute()
		{}

		public FormulaAttribute( string name, string tooltip, string author )
		{
			this.name = name;
			this.tooltip = tooltip;
			this.author = author;
		}

		public override string ToString ()
		{
			return string.Format ("name:{0}\ntooltip:{1}\nauthor:{2}", name, tooltip, author);
		}
	}
}