using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
