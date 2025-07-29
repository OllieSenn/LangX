using LangX.Data;
using LangX.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace LangX.Pages
{
    public class LanguageExercisesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public LanguageExercisesModel(ApplicationDbContext context)
        {
            _context = context;
        }
        public class ExerciseQuestion
        {
            public string Text { get; set; } = string.Empty;
            public string Answer { get; set; } = string.Empty;
            public string Hint { get; set; } = string.Empty;
        }

        [BindProperty]
        public string Topic { get; set; } = string.Empty;

        [BindProperty]
        public List<ExerciseQuestion> CurrentExercises { get; set; } = new List<ExerciseQuestion>();

        [BindProperty]
        public List<string> UserAnswers { get; set; } = new List<string>();

        // Hidden fields to preserve question data across postbacks
        [BindProperty]
        public List<string> QuestionTexts { get; set; } = new List<string>();

        [BindProperty]
        public List<string> QuestionAnswers { get; set; } = new List<string>();

        [BindProperty]
        public List<string> QuestionHints { get; set; } = new List<string>();

        public int Score { get; set; }
        public bool ShowResults { get; set; } = false;
        public bool ShowHints { get; set; } = false;

        public List<(string Question, string CorrectAnswer, string UserAnswer)> IncorrectAnswers { get; set; } = new();

        //questions hard coded in for the purpose of the demo - in an ideal world they would be stored in the db and randomised so each time they do an exercise its different
        private Dictionary<string, List<ExerciseQuestion>> ExerciseBank = new()
        {
            ["food"] = new List<ExerciseQuestion>
            {
                new() { Text = "Je voudrais un café, s'il vous plaît", Answer = "I would like a coffee, please", Hint = "Polite request for a beverage" },
                new() { Text = "Cette soupe est très épicée", Answer = "This soup is very spicy", Hint = "Describing the flavor of a dish" },
                new() { Text = "Pouvez-vous me recommander un plat ?", Answer = "Can you recommend a dish?", Hint = "Asking for a food suggestion" },
                new() { Text = "Je suis végétarien", Answer = "I am vegetarian", Hint = "Stating a dietary preference" },
                new() { Text = "L'addition, s'il vous plaît", Answer = "The bill, please", Hint = "Asking for the check at a restaurant" },
                new() { Text = "Ce gâteau est délicieux", Answer = "This cake is delicious", Hint = "Expressing enjoyment of a dessert" }
            },
            ["travel"] = new List<ExerciseQuestion>
            {
                new() { Text = "Je cherche l'hôtel Bellevue", Answer = "I am looking for the Bellevue hotel", Hint = "Asking about accommodation" },
                new() { Text = "À quelle heure part le train ?", Answer = "What time does the train leave?", Hint = "Asking about departure time" },
                new() { Text = "Pouvez-vous m'indiquer le chemin ?", Answer = "Can you show me the way?", Hint = "Asking for directions" },
                new() { Text = "Je voudrais louer une voiture", Answer = "I would like to rent a car", Hint = "Talking about transportation needs" },
                new() { Text = "Combien coûte le billet d'avion ?", Answer = "How much is the plane ticket?", Hint = "Asking about prices" },
                new() { Text = "Nous allons visiter le musée", Answer = "We are going to visit the museum", Hint = "Talking about sightseeing plans" }
            },
            ["music"] = new List<ExerciseQuestion>
            {
                new() { Text = "J'adore cette chanson", Answer = "I love this song", Hint = "Expressing enjoyment of music" },
                new() { Text = "Quel est votre groupe préféré ?", Answer = "What is your favorite band?", Hint = "Asking about music preferences" },
                new() { Text = "Je joue de la guitare depuis cinq ans", Answer = "I have been playing the guitar for five years", Hint = "Talking about musical experience" },
                new() { Text = "Le concert était incroyable", Answer = "The concert was amazing", Hint = "Describing a music event" },
                new() { Text = "Pouvez-vous baisser le volume ?", Answer = "Can you lower the volume?", Hint = "Making a request about sound" },
                new() { Text = "Cette mélodie me rappelle mon enfance", Answer = "This melody reminds me of my childhood", Hint = "Discussing emotional connection to music" }
            }
        };

        public void OnGet()
        {
            ShowResults = false;
            ShowHints = false;
            CurrentExercises.Clear();
            UserAnswers.Clear();
            QuestionTexts.Clear();
            QuestionAnswers.Clear();
            QuestionHints.Clear();
        }

        public IActionResult OnPostStartExercise()
        {
            if (!ExerciseBank.ContainsKey(Topic.ToLower()))
            {
                ModelState.AddModelError(string.Empty, "Invalid topic selected.");
                return Page();
            }

            // Get exercises for the selected topic
            var exercises = ExerciseBank[Topic.ToLower()];

            // Store exercises in the CurrentExercises list for rendering
            CurrentExercises = exercises;

            // Also store question texts, answers, and hints in separate lists for preservation during postback
            QuestionTexts = exercises.Select(q => q.Text).ToList();
            QuestionAnswers = exercises.Select(q => q.Answer).ToList();
            QuestionHints = exercises.Select(q => q.Hint).ToList();

            // Initialize user answers array
            UserAnswers = new List<string>(new string[exercises.Count]);

            return Page();
        }

        public IActionResult OnPostShowHints()
        {
            // Reconstruct CurrentExercises from hidden fields
            ReconstructExercises();

            ShowHints = true;
            return Page();
        }

        public IActionResult OnPostSubmitExercise()
        {
            // Reconstruct CurrentExercises from hidden fields
            ReconstructExercises();

            Score = 0;
            IncorrectAnswers.Clear();

            for (int i = 0; i < CurrentExercises.Count; i++)
            {
                if (i >= UserAnswers.Count || string.IsNullOrEmpty(UserAnswers[i]))
                {
                    IncorrectAnswers.Add((CurrentExercises[i].Text, CurrentExercises[i].Answer, "No answer provided"));
                    continue;
                }

                // Compare answers (case-insensitive and ignoring extra spaces as testing showed having the right answer with white space was wrong)
                string userAnswer = UserAnswers[i].Trim().ToLowerInvariant();
                string correctAnswer = CurrentExercises[i].Answer.Trim().ToLowerInvariant();

                if (userAnswer == correctAnswer)
                {
                    Score++;
                }
                else
                {
                    IncorrectAnswers.Add((CurrentExercises[i].Text, CurrentExercises[i].Answer, UserAnswers[i]));
                }
            }

            ShowResults = true;
            return Page();
        }

        public async Task<IActionResult> OnPostShareQuestionAsync(string questionText)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            // Get the current user's information
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;

            // Create the post content with the predefined format
            string postContent = $"Hey, I need help on this exercise! \"{questionText}\"";

            var post = new Post
            {
                UserId = userId,
                UserName = userName,
                Content = postContent,
                ImagePath = "",
                CreatedAt = DateTime.Now,
                LikesCount = 0
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Set a temporary message to show on redirect to show user it has been successfully shared
            TempData["SharedQuestion"] = "Your question has been shared! Check your profile or the home page to see your post.";

            return RedirectToPage("/Home");
        }
        //Rebuilds CurrentExercises from hiddenfields after postback as complex objects i.e List<ExerciseQuestion> don't survive form submissions reliably
        private void ReconstructExercises()
        {
            CurrentExercises = new List<ExerciseQuestion>();
            for (int i = 0; i < QuestionTexts.Count; i++)
            {
                CurrentExercises.Add(new ExerciseQuestion
                {
                    Text = QuestionTexts[i],
                    Answer = QuestionAnswers[i],
                    Hint = QuestionHints[i]
                });
            }
        }
    }
}