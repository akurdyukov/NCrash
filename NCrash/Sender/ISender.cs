using System.IO;
using NCrash.Core;

namespace NCrash.Sender
{
    public interface ISender
    {
        bool Send(Stream data, string fileName, Report report);
    }
}
