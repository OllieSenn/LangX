using System.ComponentModel.DataAnnotations;

namespace LangX.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Password { get; set; }  // Stores hashed password

        [Required]
        [EmailAddress]
        public string Email { get; set; }

    }
}
