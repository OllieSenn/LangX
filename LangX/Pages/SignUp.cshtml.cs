using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LangX.Data;
using LangX.Models;  
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

//Signup was made as this was my dissertation project but the signup button has been removed from landing page for the demo
namespace LangX.Pages
{
    public class SignUpModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SignUpModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            // Check if the username already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == Username);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Username already exists. Please choose another.");
                return Page();
            }

            // Hash the password before storing
            string hashedPassword = HashPassword(Password);

            // Create new user
            var newUser = new LangX.Models.User
            {
                Username = Username,
                Name = Name,
                Password = hashedPassword,
                Email = Email
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Redirect to Login Page (Index.cshtml)
            return RedirectToPage("/Index");
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