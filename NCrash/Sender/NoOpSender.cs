using System.IO;
using Common.Logging;
using NCrash.Core;

namespace NCrash.Sender
{
    /// <summary>
    /// No operational sender just logs the fact that report existed.
    /// </summary>
    public class NoOpSender : ISender
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        public bool Send(Stream data, string fileName, Report report)
        {
            Logger.WarnFormat("Report {0} removed by NoOpSender. Report was {1}", fileName, report);
            return false;
        }
    }
}
