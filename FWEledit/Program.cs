using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FWEledit
{
	static class Program
	{
		public static bool SuppressStartupRestore { get; private set; }

		/// <summary>
		/// ??????? ????? ????? ??? ??????????.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			SuppressStartupRestore = HasArgument(args, "--empty-instance") || HasArgument(args, "--no-restore");
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainWindow());
		}

		private static bool HasArgument(string[] args, string argument)
		{
			if (args == null)
			{
				return false;
			}

			for (int i = 0; i < args.Length; i++)
			{
				if (string.Equals(args[i], argument, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
	}
}
