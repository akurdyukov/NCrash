using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCrash.Plugins
{
    /// <summary>
    /// User can release any plugin, to do some actions before and after writing zip report file
    /// For example, user can make screenshot writing, memory dump or plugin to add some additional files.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Actions before writing report
        /// </summary>
        /// <param name="settings"></param>
        void PreProcess(ISettings settings);
        /// <summary>
        /// Actions after writing report
        /// </summary>
        /// <param name="settings"></param>
        void PostProcess(ISettings settings);
    }
}
