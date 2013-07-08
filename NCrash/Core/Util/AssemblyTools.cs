using System.Reflection;

namespace NCrash.Core.Util
{
    internal static class AssemblyTools
    {
        internal static Assembly EntryAssembly
        {
            get { return Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly(); }
        }
    }
}
