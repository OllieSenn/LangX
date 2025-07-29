using LangX.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using LangX.Models;

namespace LangX.Pages
{
    public class ProficiencyQuizModel : PageModel
    {

        private readonly ApplicationDbContext _context;

        // Constructor with dependency injection
        public ProficiencyQuizModel(ApplicationDbContext context)
        {
            _context = context;
        }
        public class QuizQuestion
        {
            public string Text { get; set; } = string.Empty;
            public string Answer { get; set; } = string.Empty;
        }

        [BindProperty]
        public string Topic { get; set; }

        [BindProperty]
        public List<QuizQuestion> CurrentQuestions { get; set; } = new List<QuizQuestion>();

        [BindProperty]
        public List<string> UserAnswers { get; set; } = new List<string>();

        // Hidden fields to preserve question data across postbacks
        [BindProperty]
        public List<string> QuestionTexts { get; set; } = new List<string>();

        [BindProperty]
        public List<string> QuestionAnswers { get; set; } = new List<string>();

        public int Score { get; set; }
        public bool ShowResults { get; set; } = false;
        public bool Passed { get; set; } = false;

        public List<(string Question, string CorrectAnswer)> IncorrectAnswers { get; set; } = new();

        //Like the exercises page questions are hard coded in for the demo
        private Dictionary<string, List<QuizQuestion>> QuestionBank = new()
        {
            ["food"] = new List<QuizQuestion>
            {
                new() { Text = "Je veux une pomme", Answer = "I want an apple" },
                new() { Text = "Le pain est chaud", Answer = "The bread is hot" },
                new() { Text = "Je mange une banane", Answer = "I am eating a banana" },
                new() { Text = "Le fromage est délicieux", Answer = "The cheese is delicious" },
                new() { Text = "Elle boit du lait", Answer = "She is drinking milk" },
                new() { Text = "Le dîner est prêt", Answer = "Dinner is ready" },
                new() { Text = "J'aime le chocolat", Answer = "I like chocolate" },
                new() { Text = "Le poisson est frais", Answer = "The fish is fresh" },
                new() { Text = "Nous cuisinons ensemble", Answer = "We are cooking together" },
                new() { Text = "Il commande une soupe", Answer = "He orders a soup" }
            },
            ["travel"] = new List<QuizQuestion>
            {
                new() { Text = "Où est la gare ?", Answer = "Where is the train station?" },
                new() { Text = "Je voudrais un billet", Answer = "I would like a ticket" },
                new() { Text = "L'aéroport est loin", Answer = "The airport is far" },
                new() { Text = "Elle cherche un hôtel", Answer = "She is looking for a hotel" },
                new() { Text = "Nous visitons Paris", Answer = "We are visiting Paris" },
                new() { Text = "Il prend le métro", Answer = "He takes the subway" },
                new() { Text = "Avez-vous une carte ?", Answer = "Do you have a map?" },
                new() { Text = "Je parle un peu français", Answer = "I speak a little French" },
                new() { Text = "Quelle est la prochaine station ?", Answer = "What is the next station?" },
                new() { Text = "Le taxi est en retard", Answer = "The taxi is late" }
            },
            ["music"] = new List<QuizQuestion>
            {
                new() { Text = "J'écoute de la musique", Answer = "I am listening to music" },
                new() { Text = "Elle joue du piano", Answer = "She plays the piano" },
                new() { Text = "Il aime le jazz", Answer = "He likes jazz" },
                new() { Text = "La chanson est belle", Answer = "The song is beautiful" },
                new() { Text = "Nous chantons ensemble", Answer = "We sing together" },
                new() { Text = "Le concert commence", Answer = "The concert is starting" },
                new() { Text = "J'ai une guitare", Answer = "I have a guitar" },
                new() { Text = "Ils dansent au rythme", Answer = "They dance to the rhythm" },
                new() { Text = "Le violon est cassé", Answer = "The violin is broken" },
                new() { Text = "Tu connais cette chanson ?", Answer = "Do you know this song?" }
            }
        };

        public void OnGet()
        {
            ShowResults = false;
            CurrentQuestions.Clear();
            UserAnswers.Clear();
            QuestionTexts.Clear();
            QuestionAnswers.Clear();
        }

        public IActionResult OnPostStartQuiz(string topic)
        {
            Topic = topic;
            if (!QuestionBank.ContainsKey(Topic.ToLower()))
            {
                ModelState.AddModelError(string.Empty, "Invalid topic selected.");
                return Page();
            }

            // Get questions for the selected topic
            var questions = QuestionBank[Topic.ToLower()];

            // Store questions in the CurrentQuestions list for rendering
            CurrentQuestions = questions;

            // Also store question texts and answers in separate lists for preservation during postback as it is necessary for form submissions as complex objs don't survive
            QuestionTexts = questions.Select(q => q.Text).ToList();
            QuestionAnswers = questions.Select(q => q.Answer).ToList();

            UserAnswers = new List<string>(new string[questions.Count]);

            return Page();
        }

        public async Task<IActionResult> OnPostSubmitQuizAsync()
        {
            Topic = Topic;

            CurrentQuestions = new List<QuizQuestion>();
            for (int i = 0; i < QuestionTexts.Count; i++)
            {
                CurrentQuestions.Add(new QuizQuestion
                {
                    Text = QuestionTexts[i],
                    Answer = QuestionAnswers[i]
                });
            }

            
            Score = 0;
            IncorrectAnswers.Clear();

            for (int i = 0; i < CurrentQuestions.Count; i++)
            {
                if (i >= UserAnswers.Count || string.IsNullOrEmpty(UserAnswers[i]))
                {
                    IncorrectAnswers.Add((CurrentQuestions[i].Text, CurrentQuestions[i].Answer));
                    continue;
                }

                string userAnswer = UserAnswers[i].Trim().ToLowerInvariant();
                string correctAnswer = CurrentQuestions[i].Answer.Trim().ToLowerInvariant();

                if (userAnswer == correctAnswer)
                {
                    Score++;
                }
                else
                {
                    IncorrectAnswers.Add((CurrentQuestions[i].Text, CurrentQuestions[i].Answer));
                }
            }

            if (Score >= 8)
            {
                Passed = true;
                if (!string.IsNullOrEmpty(Topic))
                {
                    await AddBadgeToUserProfile(Topic);
                }
            }
            else
            {
                Passed = false;
            }
            ShowResults = true;
            return Page();
        }

        private async Task AddBadgeToUserProfile(string topic)
        {
            if (!User.Identity.IsAuthenticated || string.IsNullOrWhiteSpace(topic))
            {
                return; 
            }

            // Normalize the topic to ensure it's valid to stop exceptions being thrown due to whitespace
            string normalizedTopic = topic.Trim();
            if (string.IsNullOrEmpty(normalizedTopic))
            {
                return; 
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingBadge = await _context.UserBadges
                .FirstOrDefaultAsync(b => b.UserId == userId && b.BadgeName.ToLower() == normalizedTopic.ToLower());

            if (existingBadge == null)
            {
                // Determine the proper icon class based on the topic so user gets the correct badge
                string iconClass = normalizedTopic.ToLower() switch
                {
                    "food" => "fas fa-utensils",
                    "travel" => "fas fa-plane",
                    "music" => "fas fa-music",
                    _ => "fas fa-award" // Default icon as a fallback
                };

                var badge = new UserBadge
                {
                    UserId = userId,
                    BadgeName = normalizedTopic,
                    BadgeDescription = $"Proficient in {normalizedTopic}",
                    AwardedDate = DateTime.Now,
                    IconClass = iconClass 
                };

                _context.UserBadges.Add(badge);
                await _context.SaveChangesAsync();
            }
        }
    }
}