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

namespace LangX.Pages.Profile
{
    public class FollowersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FollowersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public User ProfileUser { get; set; }
        public List<User> Users { get; set; } = new List<User>();
        public Dictionary<int, bool> UserFollowStatuses { get; set; } = new Dictionary<int, bool>();
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public string CurrentTab { get; set; } = "followers"; // Default tab

        public async Task<IActionResult> OnGetAsync(string username, string tab = "followers")
        {
            if (string.IsNullOrEmpty(username))
            {
                return NotFound();
            }

            // Find the user
            ProfileUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (ProfileUser == null)
            {
                return NotFound();
            }

            string profileUserId = ProfileUser.Id.ToString();
            CurrentTab = tab.ToLower() == "following" ? "following" : "followers";

            // Get followers/following counts
            FollowersCount = await _context.UserFollows
                .CountAsync(f => f.FollowingId == profileUserId);

            FollowingCount = await _context.UserFollows
                .CountAsync(f => f.FollowerId == profileUserId);

            // Load appropriate users based on the tab
            if (CurrentTab == "followers")
            {
                // Get users who follow this profile
                var followerIds = await _context.UserFollows
                    .Where(f => f.FollowingId == profileUserId)
                    .Select(f => f.FollowerId)
                    .ToListAsync();

                Users = await _context.Users
                    .Where(u => followerIds.Contains(u.Id.ToString()))
                    .ToListAsync();
            }
            else // "following"
            {
                // Get users this profile follows
                var followingIds = await _context.UserFollows
                    .Where(f => f.FollowerId == profileUserId)
                    .Select(f => f.FollowingId)
                    .ToListAsync();

                Users = await _context.Users
                    .Where(u => followingIds.Contains(u.Id.ToString()))
                    .ToListAsync();
            }

            // If current user is logged in, determine follow status for each displayed user
            if (User.Identity.IsAuthenticated)
            {
                string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userIds = Users.Select(u => u.Id.ToString()).ToList();

                var followedUsers = await _context.UserFollows
                    .Where(f => f.FollowerId == currentUserId && userIds.Contains(f.FollowingId))
                    .Select(f => f.FollowingId)
                    .ToListAsync();

                foreach (var user in Users)
                {
                    UserFollowStatuses[user.Id] = followedUsers.Contains(user.Id.ToString());
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostFollowAsync(string username, string tab, string targetUsername)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == targetUsername);

            if (targetUser == null)
            {
                return NotFound();
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string targetUserId = targetUser.Id.ToString();

            // Don't allow following yourself
            if (currentUserId == targetUserId)
            {
                return RedirectToPage("/Profile/Followers", new { username, tab });
            }

            // Check if already following
            var existingFollow = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

            if (existingFollow == null)
            {
                // Create new follow relationship
                var follow = new UserFollow
                {
                    FollowerId = currentUserId,
                    FollowingId = targetUserId,
                    CreatedAt = DateTime.Now
                };

                _context.UserFollows.Add(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Profile/Followers", new { username, tab });
        }

        public async Task<IActionResult> OnPostUnfollowAsync(string username, string tab, string targetUsername)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == targetUsername);

            if (targetUser == null)
            {
                return NotFound();
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string targetUserId = targetUser.Id.ToString();

            // Find and remove the follow relationship
            var follow = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

            if (follow != null)
            {
                _context.UserFollows.Remove(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Profile/Followers", new { username, tab });
        }
    }
}