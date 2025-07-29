using System.ComponentModel.DataAnnotations;

namespace LangX.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int PostId { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }

    
        public Post Post { get; set; }
    }
}
