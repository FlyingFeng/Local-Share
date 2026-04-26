using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.DataContext.Entities
{
    [Table("ReceiveFileTask")]
    public class ReceiveFileTaskEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("SendNodeName")]
        public string SendNodeName { get; set; } = string.Empty;
        [Column("ReceiveNodeName")]
        public string ReceiveNodeName { get; set; } = string.Empty;
        [Column("TaskId")]
        public string TaskId { get; set; } = string.Empty;
        [Column("FileFullName")]
        public string FileFullName { get; set; } = string.Empty;
        [Column("FileName")]
        public string FileName { get; set; } = string.Empty;
        /// <summary>
        /// 0:wait for schedule
        /// 1:sending/receiving
        /// 2:stop
        /// 3:finish
        /// 4:error
        /// </summary>
        [Column("State")]
        public int State { get; set; }
        [Column("SendIpAddress")]
        public string SendIpAddress { get; set; } = string.Empty;
        [Column("ReceiveIpAddress")]
        public string ReceiveIpAddress { get; set; } = string.Empty;
        [Column("InitTime")]
        public DateTime InitTime { get; set; }
        [Column("LastUpdateTime")]
        public DateTime LastUpdateTime { get; set; }
    }
}
