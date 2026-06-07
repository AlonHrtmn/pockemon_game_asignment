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
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        public class AddToTeamRequest
        {
            public int PokemonId { get; set; }
            public int SlotIndex { get; set; }
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

        [HttpGet]
        public async Task<IActionResult> GetTeam()
        {
            try
            {
                int userId = GetCurrentUserId();
                var team = await _teamService.GetTeamAsync(userId);
                return Ok(team);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToTeam([FromBody] AddToTeamRequest request)
        {
            try
            {
                int userId = GetCurrentUserId();

                if (request.SlotIndex < 0 || request.SlotIndex > 4)
                {
                    return BadRequest(new { Message = "Slot index must be between 0 and 4." });
                }

                var member = await _teamService.AddToTeamAsync(userId, request.PokemonId, request.SlotIndex);
                if (member == null)
                {
                    return NotFound(new { Message = $"Pokemon with ID {request.PokemonId} not found." });
                }

                return Ok(new { Message = "Pokemon added to team successfully.", Member = member });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new { Message = "A concurrency conflict occurred. The selected slot or Pokemon has already been updated. Please refresh and try again." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }

        [HttpDelete("pokemon/{pokemonId:int}")]
        public async Task<IActionResult> RemoveFromTeam(int pokemonId)
        {
            try
            {
                int userId = GetCurrentUserId();
                var result = await _teamService.RemoveFromTeamAsync(userId, pokemonId);
                if (!result)
                {
                    return NotFound(new { Message = $"Pokemon with ID {pokemonId} is not in your team." });
                }
                return Ok(new { Message = "Pokemon removed from team successfully." });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new { Message = "A concurrency conflict occurred. Please try again." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }

        [HttpDelete("slot/{slotIndex:int}")]
        public async Task<IActionResult> RemoveFromSlot(int slotIndex)
        {
            try
            {
                int userId = GetCurrentUserId();
                var result = await _teamService.RemoveFromSlotAsync(userId, slotIndex);
                if (!result)
                {
                    return NotFound(new { Message = $"No Pokemon found in slot {slotIndex}." });
                }
                return Ok(new { Message = $"Slot {slotIndex} cleared successfully." });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new { Message = "A concurrency conflict occurred. Please try again." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearTeam()
        {
            try
            {
                int userId = GetCurrentUserId();
                var result = await _teamService.ClearTeamAsync(userId);
                if (!result)
                {
                    return BadRequest(new { Message = "Your team is already empty." });
                }
                return Ok(new { Message = "Dream team cleared successfully." });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new { Message = "A concurrency conflict occurred. Please try again." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }
    }
}
