using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.DataContext.Entities
{
    [Table("LocalSetting")]
    public class LocalSettingEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("Key")]
        public string Key { get; set; } = string.Empty;
        [Column("Value")]
        public string Value { get; set; } = string.Empty;
    }
}
