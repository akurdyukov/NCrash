using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCrash.Plugins
{
    public interface IPlugin
    {
        void PreProcess(ISettings settings);
        void PostProcess(ISettings settings);
    }
}
