using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LangX.Data;
using LangX.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace LangX.Pages
{
    public class ViewUserModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int PostsPerPage = 10;

        public ViewUserModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public User ViewedUser { get; set; }
        public List<Post> UserPosts { get; set; } = new List<Post>();
        public bool IsFollowing { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public bool HasMorePosts { get; set; }
        public int CurrentPage { get; set; } = 0;

        public async Task<IActionResult> OnGetAsync(string username, int page = 0)
        {
            if (string.IsNullOrEmpty(username))
            {
                return NotFound();
            }

            ViewedUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (ViewedUser == null)
            {
                return NotFound();
            }

            string viewedUserId = ViewedUser.Id.ToString();

            // Set current page for pagination when viewing user
            CurrentPage = Math.Max(0, page);

            var skip = CurrentPage * PostsPerPage;

            UserPosts = await _context.Posts
                .Where(p => p.UserId == viewedUserId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(PostsPerPage + 1) 
                .Include(p => p.Comments)
                .ToListAsync();

            HasMorePosts = UserPosts.Count > PostsPerPage;

            if (HasMorePosts)
            {
                UserPosts.RemoveAt(UserPosts.Count - 1);
            }

            FollowersCount = await _context.UserFollows
                .CountAsync(f => f.FollowingId == viewedUserId);

            FollowingCount = await _context.UserFollows
                .CountAsync(f => f.FollowerId == viewedUserId);

            if (User.Identity.IsAuthenticated)
            {
                string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                IsFollowing = await _context.UserFollows
                    .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == viewedUserId);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostFollowAsync(string username)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var userToFollow = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (userToFollow == null)
            {
                return NotFound();
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string targetUserId = userToFollow.Id.ToString();

            if (currentUserId == targetUserId)
            {
                return RedirectToPage("/ViewUser", new { username });
            }

            var existingFollow = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

            if (existingFollow == null)
            {
                var follow = new UserFollow
                {
                    FollowerId = currentUserId,
                    FollowingId = targetUserId,
                    CreatedAt = DateTime.Now
                };

                _context.UserFollows.Add(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/ViewUser", new { username });
        }

        public async Task<IActionResult> OnPostUnfollowAsync(string username)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var userToUnfollow = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (userToUnfollow == null)
            {
                return NotFound();
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string targetUserId = userToUnfollow.Id.ToString();

            // Find and remove the follow relationship
            var follow = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

            if (follow != null)
            {
                _context.UserFollows.Remove(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/ViewUser", new { username });
        }

        public async Task<IActionResult> OnGetMorePostsAsync(string username, int page)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound();
            }

            string userId = user.Id.ToString();

            var skip = page * PostsPerPage;

            var posts = await _context.Posts
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(PostsPerPage + 1)
                .Include(p => p.Comments)
                .ToListAsync();

            bool hasMore = posts.Count > PostsPerPage;

            if (hasMore)
            {
                posts.RemoveAt(posts.Count - 1);
            }

            var postsData = posts.Select(p => new
            {
                id = p.Id,
                userId = p.UserId,
                userName = p.UserName,
                content = p.Content,
                imagePath = p.ImagePath,
                createdAt = p.CreatedAt,
                likesCount = p.LikesCount,
                comments = p.Comments?.Select(c => new
                {
                    id = c.Id,
                    userId = c.UserId,
                    userName = c.UserName,
                    content = c.Content,
                    createdAt = c.CreatedAt
                }).ToList()
            }).ToList();

            return new JsonResult(new
            {
                hasMorePosts = hasMore,
                posts = postsData
            });
        }
    }
}