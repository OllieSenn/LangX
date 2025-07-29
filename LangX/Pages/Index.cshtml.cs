using LangX.Data;
using LangX.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;


namespace LangX.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }


        public async Task<IActionResult> OnPostLogOnAsync()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == Username);
            string hashedPassword = HashPassword(Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Username does not exist.");
                return Page();
            }

            if (user.Password == null || user.Password != hashedPassword)
            {
                ModelState.AddModelError(string.Empty, "Incorrect password.");
                return Page();
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign the user in
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            Console.WriteLine($"Authentication successful for user: {user.Username}");
            return RedirectToPage("/Home");
        }
        public async Task<IActionResult> OnPostDemoLogOn()
        {
            Username = "demo";
            Password = "1234";
            return await OnPostLogOnAsync(); 
        }
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

    }
}

