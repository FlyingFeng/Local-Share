using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.DataContext.Entities
{
    [Table("LocalFile")]
    public class LocalFileEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("FileName")]
        public string FileName { get; set; } = string.Empty;
        [Column("FileExt")]
        public string FileExt { get; set; } = string.Empty;
        [Column("IsOpenFromDir")]
        public bool IsOpenFromDir { get; set; }
        [Column("FileSize")]
        public long FileSize { get; set; }
        [Column("FileFullPath")]
        public string FileFullPath { get; set; } = string.Empty;
        [Column("MD5")]
        public string MD5 { get; set; } = string.Empty;
        [Column("InitTime")]
        public DateTime InitTime { get; set; }

    }
}
