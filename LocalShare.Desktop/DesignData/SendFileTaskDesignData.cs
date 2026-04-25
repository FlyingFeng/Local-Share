using LocalShare.Desktop.Models.Sends;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.DesignData
{
    public class SendFileTaskDesignData
    {

        public List<FileTaskModel> CurrentNodeFileTasks { get; set; } = [];

        public SendFileTaskDesignData()
        {
            CurrentNodeFileTasks.Add(new FileTaskModel
            {
                CurrentSize = 0,
                FileName = "a.txt",
                FullFileName = "D:\\1\\2\\a.txt",
                IsOpenFromDir = true,
                Md5 = "1212121",
                Progress = 10,
                State = 1,
                TaskId = Guid.NewGuid().ToString(),
                TotalSize = 123456782
            });
        }


    }
}
