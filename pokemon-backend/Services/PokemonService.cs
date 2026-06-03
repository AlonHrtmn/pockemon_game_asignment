using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pokemon_backend.Data;
using pokemon_backend.Models;

namespace pokemon_backend.Services
{
    public class PokemonService : IPokemonService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PokemonService> _logger;

        // Hardcoded list of the original 151 Pokemons for high performance list loading and offline fallback
        private static readonly string[] PokemonNames = new[]
        {
            "bulbasaur", "ivysaur", "venusaur", "charmander", "charmeleon", "charizard",
            "squirtle", "wartortle", "blastoise", "caterpie", "metapod", "butterfree",
            "weedle", "kakuna", "beedrill", "pidgey", "pidgeotto", "pidgeot",
            "rattata", "raticate", "spearow", "fearow", "ekans", "arbok",
            "pikachu", "raichu", "sandshrew", "sandslash", "nidoran-f", "nidorina",
            "nidoqueen", "nidoran-m", "nidorino", "nidoking", "clefairy", "clefable",
            "vulpix", "ninetales", "jigglypuff", "wigglytuff", "zubat", "golbat",
            "oddish", "gloom", "vileplume", "paras", "parasect", "venonat",
            "venomoth", "diglett", "dugtrio", "meowth", "persian", "psyduck",
            "golduck", "mankey", "primeape", "growlithe", "arcanine", "poliwag",
            "poliwhirl", "poliwrath", "abra", "kadabra", "alakazam", "machop",
            "machoke", "machamp", "bellsprout", "weepinbell", "victreebel", "tentacool",
            "tentacruel", "geodude", "graveler", "golem", "ponyta", "rapidash",
            "slowpoke", "slowbro", "magnemite", "magneton", "farfetchd", "doduo",
            "dodrio", "seel", "dewgong", "grimer", "muk", "shellder",
            "cloyster", "gastly", "haunter", "gengar", "onix", "drowzee",
            "hypno", "krabby", "kingler", "voltorb", "electrode", "exeggcute",
            "exeggutor", "cubone", "marowak", "hitmonlee", "hitmonchan", "lickitung",
            "koffing", "weezing", "rhyhorn", "rhydon", "chansey", "tangela",
            "kangaskhan", "horsea", "seadra", "goldeen", "seaking", "staryu",
            "starmie", "mr-mime", "scyther", "jynx", "electabuzz", "magmar",
            "pinsir", "tauros", "magikarp", "gyarados", "lapras", "ditto",
            "eevee", "vaporeon", "jolteon", "flareon", "porygon", "omanyte",
            "omastar", "kabuto", "kabutops", "aerodactyl", "snorlax", "articuno",
            "zapdos", "moltres", "dratini", "dragonair", "dragonite", "mewtwo", "mew"
        };

        // Standard elemental types mapping for mock generation
        private static readonly Dictionary<string, string[]> PokemonTypesMock = new()
        {
            { "grass", new[] { "bulbasaur", "ivysaur", "venusaur", "oddish", "gloom", "vileplume", "bellsprout", "weepinbell", "victreebel", "tangela", "paras", "parasect" } },
            { "fire", new[] { "charmander", "charmeleon", "charizard", "vulpix", "ninetales", "growlithe", "arcanine", "ponyta", "rapidash", "magmar", "flareon", "moltres" } },
            { "water", new[] { "squirtle", "wartortle", "blastoise", "psyduck", "golduck", "poliwag", "poliwhirl", "poliwrath", "seel", "dewgong", "shellder", "cloyster", "krabby", "kingler", "horsea", "seadra", "goldeen", "seaking", "staryu", "starmie", "magikarp", "gyarados", "lapras", "vaporeon" } },
            { "electric", new[] { "pikachu", "raichu", "magnemite", "magneton", "voltorb", "electrode", "electabuzz", "jolteon", "zapdos" } },
            { "poison", new[] { "ekans", "arbok", "nidoran-f", "nidorina", "nidoqueen", "nidoran-m", "nidorino", "nidoking", "zubat", "golbat", "grimer", "muk", "koffing", "weezing" } },
            { "psychic", new[] { "abra", "kadabra", "alakazam", "drowzee", "hypno", "exeggcute", "exeggutor", "mr-mime", "jynx", "mewtwo", "mew" } },
            { "normal", new[] { "pidgey", "pidgeotto", "pidgeot", "rattata", "raticate", "spearow", "fearow", "meowth", "persian", "farfetchd", "doduo", "dodrio", "lickitung", "chansey", "kangaskhan", "tauros", "ditto", "eevee", "porygon", "snorlax" } }
        };

        public PokemonService(AppDbContext context, HttpClient httpClient, ILogger<PokemonService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(5); // Fast timeout for responsiveness
        }

        public async Task<List<PokemonListItemDto>> GetAllPokemonsAsync()
        {
            // Serve the list immediately from memory/code for maximum performance
            return PokemonNames.Select((name, index) =>
            {
                int id = index + 1;
                var types = GetMockTypes(id, name);
                return new PokemonListItemDto
                {
                    Id = id,
                    Name = char.ToUpper(name[0]) + name.Substring(1),
                    SpriteUrl = GetOfficialArtworkUrl(id),
                    Type1 = types.type1,
                    Type2 = types.type2
                };
            }).ToList();
        }

        public async Task<PokemonDetailsDto?> GetPokemonDetailsAsync(int id)
        {
            if (id < 1 || id > 151)
            {
                return null;
            }

            string name = PokemonNames[id - 1];

            // 1. Check Database Cache
            try
            {
                var cached = await _context.PokemonCache.FirstOrDefaultAsync(p => p.PokemonId == id);
                if (cached != null)
                {
                    // Refresh cache if older than 24 hours (optional, but keep it fresh)
                    if (DateTime.UtcNow - cached.LastUpdatedAt < TimeSpan.FromHours(24))
                    {
                        return JsonSerializer.Deserialize<PokemonDetailsDto>(cached.DetailsJson);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database cache query failed. Falling back to API/Mock.");
            }

            // 2. Fetch from PokeAPI
            try
            {
                var response = await _httpClient.GetAsync($"https://pokeapi.co/api/v2/pokemon/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var apiData = await response.Content.ReadFromJsonAsync<PokeApiDetails>();
                    if (apiData != null)
                    {
                        var details = MapApiDataToDto(apiData);
                        
                        // Save to Database Cache
                        await SaveToCacheAsync(id, details);

                        return details;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to fetch Pokemon {id} from API. Using local mock generator.");
            }

            // 3. Fallback to Local Database Cache (even if expired) or Mock Generator
            try
            {
                var cached = await _context.PokemonCache.FirstOrDefaultAsync(p => p.PokemonId == id);
                if (cached != null)
                {
                    _logger.LogInformation($"Serving expired cache for Pokemon {id} due to API unavailability.");
                    return JsonSerializer.Deserialize<PokemonDetailsDto>(cached.DetailsJson);
                }
            }
            catch {}

            return GenerateMockDetails(id, name);
        }

        public async Task<PokemonDetailsDto?> GetPokemonDetailsAsync(string name)
        {
            string cleanName = name.Trim().ToLower();
            int index = Array.IndexOf(PokemonNames, cleanName);
            if (index == -1)
            {
                return null;
            }
            return await GetPokemonDetailsAsync(index + 1);
        }

        private async Task SaveToCacheAsync(int id, PokemonDetailsDto details)
        {
            try
            {
                var existing = await _context.PokemonCache.FirstOrDefaultAsync(p => p.PokemonId == id);
                var json = JsonSerializer.Serialize(details);

                if (existing != null)
                {
                    existing.DetailsJson = json;
                    existing.LastUpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.PokemonCache.Add(new PokemonCacheItem
                    {
                        PokemonId = id,
                        Name = details.Name.ToLower(),
                        DetailsJson = json,
                        LastUpdatedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to write Pokemon {id} to DB cache.");
            }
        }

        private string GetOfficialArtworkUrl(int id)
        {
            return $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/{id}.png";
        }

        private (string type1, string? type2) GetMockTypes(int id, string name)
        {
            string type1 = "normal";
            string? type2 = null;

            foreach (var kvp in PokemonTypesMock)
            {
                if (kvp.Value.Contains(name))
                {
                    type1 = kvp.Key;
                    break;
                }
            }

            // Dual types for specific iconic ones
            if (name == "bulbasaur" || name == "ivysaur" || name == "venusaur") type2 = "poison";
            else if (name == "charizard") type2 = "flying";
            else if (name == "butterfree") type2 = "flying";
            else if (name == "weedle" || name == "kakuna" || name == "beedrill") type2 = "poison";
            else if (name == "pidgey" || name == "pidgeotto" || name == "pidgeot") type2 = "flying";
            else if (name == "zubat" || name == "golbat") type2 = "flying";
            else if (name == "oddish" || name == "gloom" || name == "vileplume") type2 = "poison";
            else if (name == "paras" || name == "parasect") type2 = "bug";
            else if (name == "bellsprout" || name == "weepinbell" || name == "victreebel") type2 = "poison";
            else if (name == "slowpoke" || name == "slowbro" || name == "starmie" || name == "jynx") type2 = "psychic";
            else if (name == "gyarados") type2 = "flying";
            else if (name == "gastly" || name == "haunter" || name == "gengar") { type1 = "ghost"; type2 = "poison"; }
            else if (name == "geodude" || name == "graveler" || name == "golem" || name == "onix") { type1 = "rock"; type2 = "ground"; }
            else if (name == "scyther") { type1 = "bug"; type2 = "flying"; }
            else if (name == "aerodactyl") { type1 = "rock"; type2 = "flying"; }
            else if (name == "dragonite") { type1 = "dragon"; type2 = "flying"; }
            else if (name == "articuno" || name == "lapras") { type1 = "ice"; type2 = "flying"; }
            else if (name == "zapdos") { type1 = "electric"; type2 = "flying"; }
            else if (name == "moltres") { type1 = "fire"; type2 = "flying"; }

            return (type1, type2);
        }

        private PokemonDetailsDto GenerateMockDetails(int id, string name)
        {
            var (type1, type2) = GetMockTypes(id, name);
            
            // Deterministic stats using ID as seed
            int seed = id * 17;
            int hp = 45 + (seed % 65);
            int attack = 45 + ((seed + 11) % 65);
            int defense = 45 + ((seed + 23) % 65);
            int spAttack = 50 + ((seed + 37) % 65);
            int spDefense = 50 + ((seed + 41) % 65);
            int speed = 40 + ((seed + 59) % 65);

            // Let's make iconic pokemons stronger
            if (name == "mewtwo" || name == "mew" || name == "dragonite" || name == "zapdos" || name == "articuno" || name == "moltres")
            {
                hp += 30; attack += 30; defense += 30; spAttack += 40; spDefense += 40; speed += 30;
            }

            var abilities = new List<string> { "Overgrow" };
            if (type1 == "fire") abilities = new List<string> { "Blaze", "Flash Fire" };
            else if (type1 == "water") abilities = new List<string> { "Torrent", "Swift Swim" };
            else if (type1 == "electric") abilities = new List<string> { "Static", "Lightning Rod" };
            else if (type1 == "normal") abilities = new List<string> { "Run Away", "Guts" };

            string formattedName = char.ToUpper(name[0]) + name.Substring(1);

            return new PokemonDetailsDto
            {
                Id = id,
                Name = formattedName,
                SpriteUrl = GetOfficialArtworkUrl(id),
                Type1 = type1,
                Type2 = type2,
                Hp = hp,
                Attack = attack,
                Defense = defense,
                SpecialAttack = spAttack,
                SpecialDefense = spDefense,
                Speed = speed,
                Abilities = abilities,
                Height = 7 + (id % 12),
                Weight = 60 + (id * 5 % 400),
                Description = $"{formattedName} is a mysterious and powerful creature of type {type1}. It is highly valued by trainers."
            };
        }

        private PokemonDetailsDto MapApiDataToDto(PokeApiDetails data)
        {
            var type1 = data.Types.FirstOrDefault(t => t.Slot == 1)?.Type.Name ?? "normal";
            var type2 = data.Types.FirstOrDefault(t => t.Slot == 2)?.Type.Name;

            int hp = data.Stats.FirstOrDefault(s => s.StatInfo.Name == "hp")?.BaseStat ?? 50;
            int attack = data.Stats.FirstOrDefault(s => s.StatInfo.Name == "attack")?.BaseStat ?? 50;
            int defense = data.Stats.FirstOrDefault(s => s.StatInfo.Name == "defense")?.BaseStat ?? 50;
            int spAttack = data.Stats.FirstOrDefault(s => s.StatInfo.Name == "special-attack")?.BaseStat ?? 50;
            int spDefense = data.Stats.FirstOrDefault(s => s.StatInfo.Name == "special-defense")?.BaseStat ?? 50;
            int speed = data.Stats.FirstOrDefault(s => s.StatInfo.Name == "speed")?.BaseStat ?? 50;

            var abilities = data.Abilities.Select(a => char.ToUpper(a.AbilityInfo.Name[0]) + a.AbilityInfo.Name.Substring(1)).ToList();

            string name = char.ToUpper(data.Name[0]) + data.Name.Substring(1);

            return new PokemonDetailsDto
            {
                Id = data.Id,
                Name = name,
                SpriteUrl = data.Sprites.Other?.OfficialArtwork?.FrontDefault ?? data.Sprites.FrontDefault ?? GetOfficialArtworkUrl(data.Id),
                Type1 = type1,
                Type2 = type2,
                Hp = hp,
                Attack = attack,
                Defense = defense,
                SpecialAttack = spAttack,
                SpecialDefense = spDefense,
                Speed = speed,
                Abilities = abilities,
                Height = data.Height,
                Weight = data.Weight,
                Description = $"{name} is a type {type1} Pokemon. Height: {data.Height / 10.0}m, Weight: {data.Weight / 10.0}kg."
            };
        }

        // Helper classes for parsing PokeAPI JSON response
        private class PokeApiDetails
        {
            [JsonPropertyName("id")] public int Id { get; set; }
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("height")] public int Height { get; set; }
            [JsonPropertyName("weight")] public int Weight { get; set; }
            [JsonPropertyName("sprites")] public PokeApiSprites Sprites { get; set; } = new();
            [JsonPropertyName("types")] public List<PokeApiTypeSlot> Types { get; set; } = new();
            [JsonPropertyName("stats")] public List<PokeApiStatSlot> Stats { get; set; } = new();
            [JsonPropertyName("abilities")] public List<PokeApiAbilitySlot> Abilities { get; set; } = new();
        }

        private class PokeApiSprites
        {
            [JsonPropertyName("front_default")] public string? FrontDefault { get; set; }
            [JsonPropertyName("other")] public PokeApiOtherSprites? Other { get; set; }
        }

        private class PokeApiOtherSprites
        {
            [JsonPropertyName("official-artwork")] public PokeApiOfficialArtwork? OfficialArtwork { get; set; }
        }

        private class PokeApiOfficialArtwork
        {
            [JsonPropertyName("front_default")] public string? FrontDefault { get; set; }
        }

        private class PokeApiTypeSlot
        {
            [JsonPropertyName("slot")] public int Slot { get; set; }
            [JsonPropertyName("type")] public PokeApiNamedResource Type { get; set; } = new();
        }

        private class PokeApiStatSlot
        {
            [JsonPropertyName("base_stat")] public int BaseStat { get; set; }
            [JsonPropertyName("stat")] public PokeApiNamedResource StatInfo { get; set; } = new();
        }

        private class PokeApiAbilitySlot
        {
            [JsonPropertyName("ability")] public PokeApiNamedResource AbilityInfo { get; set; } = new();
        }

        private class PokeApiNamedResource
        {
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        }
    }
}
