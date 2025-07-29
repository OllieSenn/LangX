git add -Ausing LangX.Data;
using LangX.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;


namespace LangX.Pages
{
    [Authorize]
    public class HomeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const int PostsPerPage = 10;

        public async Task<IActionResult> OnPostLogOutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }


        public List<Post> Posts { get; set; } = new List<Post>();

        [BindProperty]
        public CreatePostViewModel NewPost { get; set; }
        public List<UserLike> UserLikes { get; set; }


        public HomeModel(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            Posts = new List<Post>();
        }

        [BindProperty]
        public CreateCommentViewModel NewComment { get; set; }

        public bool HasMorePosts { get; set; }

        //This is used for the intital loading of the home page
        public async Task OnGetAsync(int page = 0)
        {
            if (!User.Identity.IsAuthenticated)
            {
                RedirectToPage("/Account/Login");
            }

            // Set the consistent PostsPerPage value to 50 for pagination - previously had infinite scroll but needed fixing so this is a simple fix for the demo
            const int PageSize = 50;

            Posts = new List<Post>();
            UserLikes = new List<UserLike>();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Calculate skip based on page number
                int skip = page * PageSize;

                // Load posts with proper pagination by using the skip int define previously
                var posts = await _context.Posts
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenBy(p => p.Id) 
                    .Skip(skip)
                    .Take(PageSize + 1) 
                    .ToListAsync();

                HasMorePosts = posts.Count > PageSize;

                if (HasMorePosts)
                {
                    posts.RemoveAt(posts.Count - 1);
                }

                Posts = posts;

                // Load comments for each post
                foreach (var post in Posts)
                {
                    post.Comments = await _context.Comments
                        .Where(c => c.PostId == post.Id)
                        .OrderBy(c => c.CreatedAt)
                        .ToListAsync();

                
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    UserLikes = await _context.UserLikes
                        .Where(ul => ul.UserId == userId)
                        .ToListAsync();
                }

                // Store current page in ViewData for pagination
                ViewData["CurrentPage"] = page;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR loading posts: {ex.Message}");
            }
        }

        public async Task<JsonResult> OnGetLoadMorePostsAsync(int page, int postsPerPage = 50)
        {
            postsPerPage = postsPerPage <= 0 ? 50 : postsPerPage;

            int skip = postsPerPage * page;

            // Query posts without including non-existent navigation properties
            var posts = await _context.Posts
                .OrderByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Id) // Secondary sort for stable ordering
                .Skip(skip)
                .Take(postsPerPage + 1) // Take one extra to check if more exist
                .ToListAsync();

            foreach (var post in posts)
            {
                post.Comments = await _context.Comments
                    .Where(c => c.PostId == post.Id)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();
            }

            bool hasMorePosts = posts.Count > postsPerPage;

            // Remove the extra post if it exists to keep it to the 50 per page
            if (hasMorePosts)
            {
                posts.RemoveAt(posts.Count - 1);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userLikes = await _context.UserLikes
                .Where(ul => ul.UserId == userId && posts.Select(p => p.Id).Contains(ul.PostId))
                .ToListAsync();

            // Map posts to DTOs without referencing the User navigation property for use in the JSON response
            var postDtos = posts.Select(p => new
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

            // Map user likes to DTOs
            var userLikeDtos = userLikes.Select(ul => new
            {
                userId = ul.UserId,
                postId = ul.PostId
            }).ToList();

            // Return JSON response
            return new JsonResult(new
            {
                posts = postDtos,
                userLikes = userLikeDtos,
                hasMorePosts,
                page,
                totalCount = skip + posts.Count + (hasMorePosts ? 1 : 0) // Estimate of total count
            });
        }
        public async Task<IActionResult> OnPostCommentAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
            {
                return Unauthorized();
            }

            var comment = new Comment
            {
                PostId = NewComment.PostId,
                UserId = userId,
                UserName = userName,
                Content = NewComment.Content,
                CreatedAt = DateTime.Now
            };

            // Save to database
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Redirect to refresh the page
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteCommentAsync(int commentId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var comment = await _context.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return NotFound();
            }

            // Check if the current user is the post owner or the comment owner
            bool isPostOwner = comment.Post?.UserId == currentUserId;
            bool isCommentOwner = comment.UserId == currentUserId;

            // If user is neither the post owner nor comment owner, don't allow deletion as we don't a users post being deleted by other users
            if (!isPostOwner && !isCommentOwner)
            {
                return Forbid();
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public IActionResult OnPostViewProfile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            string username = User.Identity.Name;

            // Redirect to profile page with username parameter so current user can view another users page
            return RedirectToPage("/Profile", new { username = username });
        }

        public IActionResult OnGetViewUserProfile(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Index");
            }

            return RedirectToPage("/ViewUser", new { username = username });
        }
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
            {
                return Unauthorized();
            }
            var post = new Post
            {
                UserId = userId,
                UserName = userName,
                Content = NewPost.Content,
                ImagePath = string.Empty,
                CreatedAt = DateTime.Now
            };

            // Handle image upload if present
            if (NewPost.Image != null && NewPost.Image.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(NewPost.Image.FileName);
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await NewPost.Image.CopyToAsync(fileStream);
                }

                post.ImagePath = "/uploads/" + fileName;
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Redirect to refresh the page so user can see the new post
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLikeAsync(int postId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new JsonResult(new { success = false, message = "User not authenticated" });
                }

                var post = await _context.Posts.FindAsync(postId);
                if (post == null)
                {
                    return new JsonResult(new { success = false, message = "Post not found" });
                }

                bool alreadyLiked = false;

                try
                {
                    var existingLike = await _context.UserLikes
                        .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.PostId == postId);

                    alreadyLiked = (existingLike != null);
                }
                catch
                {
                    alreadyLiked = false;
                }

                if (alreadyLiked)
                {
                    return new JsonResult(new { success = true, likesCount = post.LikesCount });
                }

                try
                {
                    _context.UserLikes.Add(new UserLike
                    {
                        UserId = userId,
                        PostId = postId,
                        CreatedAt = DateTime.Now
                    });
                }
                catch
                {
                    // If table doesn't exist, just increment the count so errors aren't caused for the sake of the demo
                }

                // Update like count regardless
                post.LikesCount++;
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, likesCount = post.LikesCount });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}
