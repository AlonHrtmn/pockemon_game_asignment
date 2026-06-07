using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pokemon_backend.Models;

namespace pokemon_backend.Services
{
    public class AiCoachService : IAiCoachService
    {
        private readonly IPokemonService _pokemonService;

        public AiCoachService(IPokemonService pokemonService)
        {
            _pokemonService = pokemonService;
        }

        public async Task<AiCoachResponseDto> AnalyzeTeamAsync(List<DreamTeamMember> team)
        {
            var response = new AiCoachResponseDto();

            if (team == null || !team.Any())
            {
                response.OverallSummary = "Your dream team is currently empty! Head back to the Pokemon database and recruit your first companion to begin your journey.";
                response.TeamStyle = "Empty Team";
                response.CoachAdvice = "Try searching for starter Pokemons like Bulbasaur, Charmander, or Squirtle to establish a solid foundation!";
                return response;
            }

            // 1. Fetch detailed stats for all team members
            var detailsList = new List<PokemonDetailsDto>();
            foreach (var member in team)
            {
                var details = await _pokemonService.GetPokemonDetailsAsync(member.PokemonId);
                if (details != null)
                {
                    detailsList.Add(details);
                }
            }

            int count = detailsList.Count;

            if (count == 0)
            {
                response.OverallSummary = "Failed to retrieve details for any Pokemon on your team. Unable to perform analysis.";
                response.TeamStyle = "Unknown";
                response.CoachAdvice = "Please make sure the Pokemon on your team have valid IDs and try again.";
                return response;
            }

            // 2. Perform Stats Analysis
            double avgHp = detailsList.Average(p => p.Hp);
            double avgAttack = detailsList.Average(p => p.Attack);
            double avgDefense = detailsList.Average(p => p.Defense);
            double avgSpAttack = detailsList.Average(p => p.SpecialAttack);
            double avgSpDefense = detailsList.Average(p => p.SpecialDefense);
            double avgSpeed = detailsList.Average(p => p.Speed);

            // Determine Team Style
            if (avgSpeed > 85)
            {
                response.TeamStyle = "Speed Blitzers";
                response.OverallSummary = "A blazingly fast squad! Your team is built to strike first and end battles before your opponents can react.";
            }
            else if (avgHp > 80 || avgDefense > 80 || avgSpDefense > 80)
            {
                response.TeamStyle = "Iron Fortress";
                response.OverallSummary = "An immovable wall! Your team has excellent bulk and defensive attributes, making them ideal for outlasting opponents.";
            }
            else if (avgAttack > 80 || avgSpAttack > 80)
            {
                response.TeamStyle = "Hyper Offense";
                response.OverallSummary = "Raw firepower! Your team packs devastating physical and special offensive capabilities, designed to crush defenses.";
            }
            else
            {
                response.TeamStyle = "Balanced Vanguard";
                response.OverallSummary = "An incredibly balanced team! You have a versatile mix of stats, allowing you to adapt to any battlefield situation.";
            }

            // 3. Perform Type Coverage & Synergy Analysis
            var types = new HashSet<string>();
            foreach (var p in detailsList)
            {
                types.Add(p.Type1.ToLower());
                if (!string.IsNullOrEmpty(p.Type2))
                {
                    types.Add(p.Type2.ToLower());
                }
            }

            // Strengths list
            foreach (var type in types)
            {
                switch (type)
                {
                    case "fire":
                        response.Strengths.Add("Excellent coverage against Grass, Bug, Steel, and Ice types.");
                        break;
                    case "water":
                        response.Strengths.Add("Strong matchup control against Fire, Ground, and Rock threats.");
                        break;
                    case "grass":
                        response.Strengths.Add("Great utility and damage against Water, Ground, and Rock types.");
                        break;
                    case "electric":
                        response.Strengths.Add("Agile offense capable of vaporizing Water and Flying opponents.");
                        break;
                    case "psychic":
                        response.Strengths.Add("High special capacity to shut down Fighting and Poison enemies.");
                        break;
                    case "ghost":
                        response.Strengths.Add("Immunities to Normal and Fighting moves, strong against Psychic.");
                        break;
                    case "dragon":
                        response.Strengths.Add("Resists all primary elemental types (Fire, Water, Grass, Electric).");
                        break;
                }
            }

            if (response.Strengths.Count == 0)
            {
                response.Strengths.Add("General physical and elemental coverage.");
            }

            // Weaknesses list (calculate simple vulnerabilities)
            var typeVulnerabilities = new Dictionary<string, int>();
            foreach (var p in detailsList)
            {
                var vulnerabilities = GetWeaknesses(p.Type1, p.Type2);
                foreach (var v in vulnerabilities)
                {
                    if (typeVulnerabilities.ContainsKey(v))
                        typeVulnerabilities[v]++;
                    else
                        typeVulnerabilities[v] = 1;
                }
            }

            // List elements where 2 or more team members are weak
            var majorVulnerabilities = typeVulnerabilities.Where(kvp => kvp.Value >= 2).Select(kvp => kvp.Key).ToList();
            foreach (var v in majorVulnerabilities)
            {
                response.Weaknesses.Add($"Shared vulnerability to {char.ToUpper(v[0]) + v.Substring(1)} attacks ({typeVulnerabilities[v]} members affected).");
            }

            if (response.Weaknesses.Count == 0)
            {
                response.Weaknesses.Add("No critical overlapping weaknesses found. Great job on type coverage!");
            }

            // Synergy Check
            bool hasFire = types.Contains("fire");
            bool hasWater = types.Contains("water");
            bool hasGrass = types.Contains("grass");

            if (hasFire && hasWater && hasGrass)
            {
                response.SynergyNotes.Add("Starter Core Unlocked! Having Fire, Water, and Grass forms a perfect elemental triangle of defensive switching.");
            }

            if (detailsList.Any(p => p.Name.ToLower() == "pikachu") && detailsList.Any(p => p.Name.ToLower() == "eevee"))
            {
                response.SynergyNotes.Add("Let's Go Duo! Partnering Pikachu and Eevee brings a fun, energetic vibe and excellent versatility.");
            }

            if (types.Count >= 6)
            {
                response.SynergyNotes.Add("High Variety: You are utilizing 6 or more elemental types! This makes you unpredictable and hard to counter.");
            }

            if (team.Count < 5)
            {
                response.SynergyNotes.Add($"Incomplete Squad: You have {team.Count}/5 members. Add more Pokemons to unlock full team analysis.");
            }

            // 4. Individual Pokemon Reviews
            foreach (var p in detailsList)
            {
                string comment = "";
                if (p.Hp > 90) comment = $"{p.Name} is your team's anchor, capable of soaking up heavy damage.";
                else if (p.Speed > 95) comment = $"{p.Name} is incredibly swift and will likely secure first-turn strikes.";
                else if (p.Attack > 95 || p.SpecialAttack > 95) comment = $"{p.Name} acts as a powerful offensive sweeper.";
                else if (p.Defense > 95 || p.SpecialDefense > 95) comment = $"{p.Name} provides sturdy defensive support.";
                else comment = $"{p.Name} offers reliable all-round stats and type utility.";

                response.IndividualReviews.Add(comment);
            }

            // 5. Coach Advice
            if (team.Count < 5)
            {
                response.CoachAdvice = $"Complete your team by adding {5 - team.Count} more Pokemons. Look for types that cover your current members!";
            }
            else
            {
                if (majorVulnerabilities.Contains("ground"))
                {
                    response.CoachAdvice = "Recommendation: You have a Ground-type weakness. Consider adding a Flying-type (like Pidgeot or Charizard) or a Grass-type to absorb Ground-type moves (which have zero effect on Flying types!).";
                }
                else if (majorVulnerabilities.Contains("electric"))
                {
                    response.CoachAdvice = "Recommendation: You have an Electric-type weakness. Adding a Ground-type Pokemon (like Diglett or Golem) will give you a complete immunity to Electric attacks!";
                }
                else if (majorVulnerabilities.Contains("water"))
                {
                    response.CoachAdvice = "Recommendation: Your team is vulnerable to Water. Recruiting an Electric-type (like Pikachu) or a Grass-type (like Bulbasaur) will help you counter water threats easily.";
                }
                else
                {
                    response.CoachAdvice = "Your team looks extremely solid! Make sure to register your team, practice your strategies, and get ready for battles. Good luck, Trainer!";
                }
            }

            return response;
        }

        private List<string> GetWeaknesses(string type1, string? type2)
        {
            var weaknesses = new List<string>();

            // Helper to get raw weaknesses
            void AddWeaknesses(string t)
            {
                switch (t.ToLower())
                {
                    case "grass":
                        weaknesses.AddRange(new[] { "fire", "ice", "poison", "flying", "bug" });
                        break;
                    case "fire":
                        weaknesses.AddRange(new[] { "water", "ground", "rock" });
                        break;
                    case "water":
                        weaknesses.AddRange(new[] { "electric", "grass" });
                        break;
                    case "electric":
                        weaknesses.AddRange(new[] { "ground" });
                        break;
                    case "poison":
                        weaknesses.AddRange(new[] { "ground", "psychic" });
                        break;
                    case "psychic":
                        weaknesses.AddRange(new[] { "bug", "ghost", "dark" });
                        break;
                    case "normal":
                        weaknesses.AddRange(new[] { "fighting" });
                        break;
                    case "flying":
                        weaknesses.AddRange(new[] { "electric", "ice", "rock" });
                        break;
                    case "bug":
                        weaknesses.AddRange(new[] { "fire", "flying", "rock" });
                        break;
                    case "ghost":
                        weaknesses.AddRange(new[] { "ghost", "dark" });
                        break;
                    case "rock":
                        weaknesses.AddRange(new[] { "water", "grass", "fighting", "ground", "steel" });
                        break;
                    case "ground":
                        weaknesses.AddRange(new[] { "water", "grass", "ice" });
                        break;
                    case "ice":
                        weaknesses.AddRange(new[] { "fire", "fighting", "rock", "steel" });
                        break;
                    case "dragon":
                        weaknesses.AddRange(new[] { "ice", "dragon", "fairy" });
                        break;
                    case "fighting":
                        weaknesses.AddRange(new[] { "flying", "psychic", "fairy" });
                        break;
                    case "steel":
                        weaknesses.AddRange(new[] { "fire", "fighting", "ground" });
                        break;
                    case "fairy":
                        weaknesses.AddRange(new[] { "poison", "steel" });
                        break;
                    case "dark":
                        weaknesses.AddRange(new[] { "fighting", "bug", "fairy" });
                        break;
                }
            }

            AddWeaknesses(type1);
            if (!string.IsNullOrEmpty(type2))
            {
                AddWeaknesses(type2);
            }

            return weaknesses.Distinct().ToList();
        }
    }
}
