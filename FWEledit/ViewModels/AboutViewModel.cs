using System;
using System.Reflection;

namespace FWEledit
{
    public sealed class AboutViewModel : ViewModelBase
    {
        public string Title { get; private set; }
        public string Product { get; private set; }
        public string Version { get; private set; }
        public string Copyright { get; private set; }
        public string Company { get; private set; }
        public string Description { get; private set; }

        public AboutViewModel()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Title = GetTitle(assembly);
            Product = GetAttribute<AssemblyProductAttribute>(assembly, a => a.Product);
            Version = assembly.GetName().Version.ToString();
            Copyright = GetAttribute<AssemblyCopyrightAttribute>(assembly, a => a.Copyright);
            Company = GetAttribute<AssemblyCompanyAttribute>(assembly, a => a.Company);
            Description = GetAttribute<AssemblyDescriptionAttribute>(assembly, a => a.Description);
        }

        private static string GetTitle(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (!string.IsNullOrWhiteSpace(titleAttribute.Title))
                {
                    return titleAttribute.Title;
                }
            }
            return System.IO.Path.GetFileNameWithoutExtension(assembly.CodeBase);
        }

        private static string GetAttribute<T>(Assembly assembly, Func<T, string> selector) where T : Attribute
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(T), false);
            if (attributes.Length == 0)
            {
                return string.Empty;
            }
            return selector((T)attributes[0]) ?? string.Empty;
        }
    }
}
