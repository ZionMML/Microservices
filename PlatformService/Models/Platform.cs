using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlatformService.Models
{
    public class Platform
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // 👈 Important
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Publisher { get; set; }

        public required string Cost { get; set; }
    }
}
