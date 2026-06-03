using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pokemon_backend.Services;

namespace pokemon_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authenticated session for all endpoints
    public class PokemonController : ControllerBase
    {
        private readonly IPokemonService _pokemonService;

        public PokemonController(IPokemonService pokemonService)
        {
            _pokemonService = pokemonService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var pokemons = await _pokemonService.GetAllPokemonsAsync();
            return Ok(pokemons);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var details = await _pokemonService.GetPokemonDetailsAsync(id);
            if (details == null)
            {
                return NotFound(new { Message = $"Pokemon with ID {id} not found." });
            }
            return Ok(details);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetByName(string name)
        {
            var details = await _pokemonService.GetPokemonDetailsAsync(name);
            if (details == null)
            {
                return NotFound(new { Message = $"Pokemon '{name}' not found." });
            }
            return Ok(details);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var all = await _pokemonService.GetAllPokemonsAsync();
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(all);
            }

            string cleanQuery = query.Trim().ToLower();
            
            // Smart matching: by name or by type
            var filtered = all.Where(p => 
                p.Name.ToLower().Contains(cleanQuery) || 
                p.Type1.ToLower() == cleanQuery || 
                (p.Type2 != null && p.Type2.ToLower() == cleanQuery)
            ).ToList();

            return Ok(filtered);
        }
    }
}
