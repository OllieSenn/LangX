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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace LangX.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int PostsPerPage = 5;

        public ProfileModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnPostLogOutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }

        public User ProfileUser { get; set; }
        public List<Post> Posts { get; set; }
        public int PostCount { get; set; }
        public int CommentCount { get; set; }
        public int TotalLikes { get; set; }
        public bool HasMorePosts { get; set; }
        public bool IsCurrentUser { get; set; }
        public bool IsFollowing { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public int CurrentPage { get; set; } = 0;
        public List<UserBadge> UserBadges { get; set; } = new List<UserBadge>();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string username, int page = 0)
        {
            if (string.IsNullOrEmpty(username))
            {
                return NotFound();
            }

            ProfileUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (ProfileUser == null)
            {
                return NotFound();
            }

            CurrentPage = Math.Max(0, page);

            // Get the user's ID as string to match with Post.UserId
            string userId = ProfileUser.Id.ToString();

            var skip = CurrentPage * PostsPerPage;

            Posts = await _context.Posts
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(PostsPerPage)
                .Include(p => p.Comments)
                .ToListAsync();

            // Get counts for stats to display to user
            PostCount = await _context.Posts.CountAsync(p => p.UserId == userId);

            // Count comments made by the user (not on their posts)
            CommentCount = await _context.Comments.CountAsync(c => c.UserId == userId);

            FollowersCount = await _context.UserFollows
                .CountAsync(f => f.FollowingId == userId);

            FollowingCount = await _context.UserFollows
                .CountAsync(f => f.FollowerId == userId);

            TotalLikes = await _context.Posts
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.LikesCount);

            // Check if current user is viewing their own profile as theres different functionality based on ownership
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IsCurrentUser = currentUserId == userId;

            if (User.Identity.IsAuthenticated && !IsCurrentUser)
            {
                IsFollowing = await _context.UserFollows
                    .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == userId);
            }

            HasMorePosts = (skip + PostsPerPage) < PostCount;

            UserBadges = await _context.UserBadges
                .Where(b => b.UserId == userId)
                .ToListAsync();

            return Page();
        }
        public async Task<IActionResult> OnPostDeletePostAsync(int postId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                return NotFound();
            }

            // Make sure the current user is the post owner so it can't be deleted by other users
            if (post.UserId != currentUserId)
            {
                return Forbid();
            }

            // Remove any comments associated with this post to clean db
            var comments = await _context.Comments
                .Where(c => c.PostId == postId)
                .ToListAsync();

            if (comments.Any())
            {
                _context.Comments.RemoveRange(comments);
            }

            // Remove any likes associated with this post, same as comments
            var likes = await _context.UserLikes
                .Where(l => l.PostId == postId)
                .ToListAsync();

            if (likes.Any())
            {
                _context.UserLikes.RemoveRange(likes);
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Profile", new { username = User.Identity.Name });
        }

        public async Task<IActionResult> OnPostEditPostAsync(int postId, string content)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                return NotFound();
            }

            if (post.UserId != currentUserId)
            {
                return Forbid();
            }

            post.Content = content;

            await _context.SaveChangesAsync();

            return RedirectToPage("/Profile", new { username = User.Identity.Name });
        }

        public IActionResult OnGetViewFollowers(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return NotFound();
            }

            return RedirectToPage("/Profile/Followers", new
            {
                username = username,
                tab = "followers"
            });
        }

        public IActionResult OnGetViewFollowing(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return NotFound();
            }

            return RedirectToPage("/Profile/Followers", new
            {
                username = username,
                tab = "following"
            });
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

            // Stops users from following themself
            if (currentUserId == targetUserId)
            {
                return RedirectToPage("/Profile", new { username });
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

            return RedirectToPage("/Profile", new { username });
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

            var follow = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

            if (follow != null)
            {
                _context.UserFollows.Remove(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Profile", new { username });
        }

        public async Task<IActionResult> OnPostEditProfileAsync(string username, string name)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id.ToString() == currentUserId);

            if (user == null)
            {
                return NotFound();
            }

            // Check if the user is trying to edit their own profile, profile ownship like the OnGetAsync() function
            if (user.Id.ToString() != currentUserId)
            {
                return Forbid();
            }

            // Check if username is changed and if the new username is already taken so we don't get duplicate usernames
            if (user.Username != username && !string.IsNullOrEmpty(username))
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (existingUser != null)
                {
                    StatusMessage = "Error: This username is already taken. Please choose another one.";
                    return RedirectToPage("/Profile", new { username = user.Username });
                }

                user.Username = username;

                // Update username in all posts by this user
                var userPosts = await _context.Posts
                    .Where(p => p.UserId == currentUserId)
                    .ToListAsync();

                foreach (var post in userPosts)
                {
                    post.UserName = username;
                }

                var userComments = await _context.Comments
                    .Where(c => c.UserId == currentUserId)
                    .ToListAsync();

                foreach (var comment in userComments)
                {
                    comment.UserName = username;
                }
            }

            if (!string.IsNullOrEmpty(name))
            {
                user.Name = name;
            }

            await _context.SaveChangesAsync();

            StatusMessage = "Your profile has been updated successfully.";
            return RedirectToPage("/Profile", new { username = user.Username });
        }
    }
}