using System;
using System.ComponentModel.DataAnnotations;

namespace LangX.Models
{
    public class UserFollow
    {
        [Key]
        public int Id { get; set; }

        // The user who is following
        [Required]
        public string FollowerId { get; set; }

        // The user who is being followed
        [Required]
        public string FollowingId { get; set; }

        // When the follow relationship was created
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}