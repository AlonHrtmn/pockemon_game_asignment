using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pokemon_backend.Services;

namespace pokemon_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authenticated session for all endpoints
    public class AiCoachController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly IAiCoachService _aiCoachService;

        public AiCoachController(ITeamService teamService, IAiCoachService aiCoachService)
        {
            _teamService = teamService;
            _aiCoachService = aiCoachService;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
            {
                throw new InvalidOperationException("User NameIdentifier claim not found.");
            }
            return int.Parse(claim.Value);
        }

        [HttpGet("analyze")]
        public async Task<IActionResult> AnalyzeTeam()
        {
            try
            {
                int userId = GetCurrentUserId();
                var team = await _teamService.GetTeamAsync(userId);
                var analysis = await _aiCoachService.AnalyzeTeamAsync(team);
                return Ok(analysis);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }
    }
}
