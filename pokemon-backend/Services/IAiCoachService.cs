using System.Collections.Generic;
using System.Threading.Tasks;
using pokemon_backend.Models;

namespace pokemon_backend.Services
{
    public class AiCoachResponseDto
    {
        public string OverallSummary { get; set; } = string.Empty;
        public string TeamStyle { get; set; } = string.Empty; // e.g. "Speed Blitzers", "Balanced Force", etc.
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> SynergyNotes { get; set; } = new();
        public string CoachAdvice { get; set; } = string.Empty;
        public List<string> IndividualReviews { get; set; } = new();
    }

    public interface IAiCoachService
    {
        Task<AiCoachResponseDto> AnalyzeTeamAsync(List<DreamTeamMember> team);
    }
}
