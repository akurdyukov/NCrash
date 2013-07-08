using NCrash.Core;

namespace NCrash.UI
{
    /// <summary>
    /// Interface for UI implementations
    /// </summary>
    public interface IUserInterface
    {
        UIDialogResult DisplayBugReportUI(Report report);
    }
}
