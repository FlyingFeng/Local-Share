using LocalShare.Desktop.DataContext.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.DataContext
{
    public class LocalDataContext : DbContext
    {
        public LocalDataContext(DbContextOptions<LocalDataContext> options) : base(options)
        {

        }

        public DbSet<LocalNodeEntity> LocalNodes { get; set; }
        public DbSet<WhiteListEntity> WhiteLists { get; set; }
        public DbSet<BlackListEntity> BlackLists { get; set; }
        public DbSet<LocalFileEntity> LocalFiles { get; set; }
        public DbSet<LocalSettingEntity> LocalSettings { get; set; }
        public DbSet<SendFileTaskEntity> SendFileTasks { get; set; }

    }
}
