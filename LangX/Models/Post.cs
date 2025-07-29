using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LangX.Models
{

    public class Post
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }

        public string Content { get; set; }

        public string ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int LikesCount { get; set; } = 0;
        public List<Comment> Comments { get; set; } = new List<Comment>();
        
        
    }

    
    public class CreatePostViewModel
    {
        
        [Required(ErrorMessage = "Please enter some content for your post")]
        public string Content { get; set; }

        public IFormFile? Image { get; set; } // Make it nullable for proper error handling
    }
    public class CreateCommentViewModel
    {
        [Required(ErrorMessage = "Comment cannot be empty")]
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string Content { get; set; }

        public int PostId { get; set; }
    }
}