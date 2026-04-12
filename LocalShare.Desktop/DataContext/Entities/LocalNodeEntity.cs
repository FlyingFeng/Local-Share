using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.DataContext.Entities
{
    [Table("LocalNode")]
    public class LocalNodeEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("NodeName")]
        public string NodeName { get; set; } = string.Empty;
        [Column("InitTime")]
        public DateTime InitTime { get; set; }
        [Column("LastUpdateTime")]
        public DateTime LastUpdateTime { get; set; }
    }
}
