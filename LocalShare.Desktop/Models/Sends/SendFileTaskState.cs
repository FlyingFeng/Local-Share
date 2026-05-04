
namespace LocalShare.Desktop.Models.Sends
{
    internal enum SendFileTaskState
    {
        WaitForSchedule,
        Transferring,
        Stop,
        Finish,
        Error
    }
}
