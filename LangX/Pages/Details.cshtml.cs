using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LangX.Data;
using LangX.Models;
using Microsoft.AspNetCore.Authentication;

namespace LangX.Pages.Posts
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Post Post { get; set; }
        public bool IsLikedByCurrentUser { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Find the post with its comments
            Post = await _context.Posts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (Post == null)
            {
                return Page();
            }

            // Check if the current user has liked this post
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                IsLikedByCurrentUser = await _context.UserLikes
                    .AnyAsync(ul => ul.UserId == userId && ul.PostId == id);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostLikePostAsync(int postId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Index");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if already liked
            var existingLike = await _context.UserLikes
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.PostId == postId);

            // Find the post
            var post = await _context.Posts.FindAsync(postId);

            if (post == null)
            {
                return NotFound();
            }

            if (existingLike == null)
            {
                // Add a new like with user ID so it can be used to identify what the current user has liked already
                _context.UserLikes.Add(new UserLike
                {
                    UserId = userId,
                    PostId = postId,
                });

                post.LikesCount++;
                await _context.SaveChangesAsync();
            }
            else
            {
                // If already liked, unlike it - not currently working needs fixing
                _context.UserLikes.Remove(existingLike);

                if (post.LikesCount > 0)
                {
                    post.LikesCount--;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Details", new { id = postId });
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int postId, string commentContent)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Index");
            }

            if (string.IsNullOrWhiteSpace(commentContent))
            {
                return RedirectToPage("/Details", new { id = postId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;

            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                UserName = userName,
                Content = commentContent,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Details", new { id = postId });
        }

        public async Task<IActionResult> OnPostDeleteCommentAsync(int commentId, int postId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Index");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the comment
            var comment = await _context.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return NotFound();
            }

            // Check if current user is the comment owner or post owner
            bool isCommentOwner = comment.UserId == userId;
            bool isPostOwner = comment.Post.UserId == userId;

            if (!isCommentOwner && !isPostOwner)
            {
                return Forbid();
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Details", new { id = postId });
        }

        public async Task<IActionResult> OnPostLogOutAsync()
        {
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Index");
        }
    }
}