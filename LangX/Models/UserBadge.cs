using System;
using System.ComponentModel.DataAnnotations;

namespace LangX.Models
{
    public class UserBadge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string BadgeName { get; set; }

        public string BadgeDescription { get; set; }

        public DateTime AwardedDate { get; set; } = DateTime.Now;

        // Badge icon class (for Font Awesome)
        public string IconClass { get; set; }

        public string GetIconClass()
        {
            // Default icon mappings based on badge name
            return BadgeName.ToLower() switch
            {
                "food" => "fas fa-utensils",
                "travel" => "fas fa-plane",
                "music" => "fas fa-music",
                _ => "fas fa-award" // Default icon
            };
        }
    }
}