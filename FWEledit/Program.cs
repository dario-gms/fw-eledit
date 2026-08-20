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
			try
			{
				Application.Run(new MainWindow());
			}
			catch (System.Configuration.ConfigurationErrorsException ex)
			{
				if (!RecoverCorruptUserConfig(ex))
				{
					throw;
				}

				Application.Run(new MainWindow());
			}
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

		private static bool RecoverCorruptUserConfig(System.Configuration.ConfigurationErrorsException ex)
		{
			string filename = ex != null ? ex.Filename : string.Empty;
			if (string.IsNullOrWhiteSpace(filename) && ex != null)
			{
				System.Configuration.ConfigurationErrorsException inner = ex.InnerException as System.Configuration.ConfigurationErrorsException;
				filename = inner != null ? inner.Filename : string.Empty;
			}

			if (string.IsNullOrWhiteSpace(filename) || !System.IO.File.Exists(filename))
			{
				return false;
			}

			try
			{
				string backup = filename + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss");
				System.IO.File.Move(filename, backup);
				System.Configuration.ConfigurationManager.RefreshSection("userSettings");
				Properties.Settings.Default.Reload();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
