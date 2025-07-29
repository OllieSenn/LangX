using System;
using System.ComponentModel.DataAnnotations;

namespace LangX.Models
{
    public class UserLike
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int PostId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}