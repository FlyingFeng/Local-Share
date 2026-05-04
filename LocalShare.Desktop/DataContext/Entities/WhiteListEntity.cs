using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalShare.Desktop.DataContext.Entities
{
    [Table("WhiteList")]
    public class WhiteListEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("NodeName")]
        public string NodeName { get; set; } = string.Empty;
        [Column("IpAddress")]
        public string IpAddress { get; set; } = string.Empty;
        [Column("Port")]
        public int Port { get; set; }
        [Column("InitTime")]
        public DateTime InitTime { get; set; }
    }
}
