using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BulkyBook.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        [DisplayName("Display Order")]
        [Range(1, 100, ErrorMessage="Display Order must be between 1 and 100")] // if the display order input is not in range from 1 to 100, it will throw an error message
        public int DisplayOrder { get; set; }
        public DateTime CreatedDateTime { get; set; } = DateTime.Now;

    }
}
