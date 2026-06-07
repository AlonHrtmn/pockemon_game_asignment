import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PokemonListItem {
  id: number;
  name: string;
  spriteUrl: string;
  type1: string;
  type2?: string;
}

export interface PokemonDetails {
  id: number;
  name: string;
  spriteUrl: string;
  type1: string;
  type2?: string;
  hp: number;
  attack: number;
  defense: number;
  specialAttack: number;
  specialDefense: number;
  speed: number;
  abilities: string[];
  height: number;
  weight: number;
  description: string;
}

export interface DreamTeamMember {
  id: number;
  userId: number;
  pokemonId: number;
  pokemonName: string;
  spriteUrl: string;
  type1: string;
  type2?: string;
  slotIndex: number;
  addedAt: string;
}

export interface AiCoachResponse {
  overallSummary: string;
  teamStyle: string;
  strengths: string[];
  weaknesses: string[];
  synergyNotes: string[];
  coachAdvice: string;
  individualReviews: string[];
}

@Injectable({
  providedIn: 'root'
})
export class PokemonService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // 1. Pokemon Endpoints
  getPokemons(): Observable<PokemonListItem[]> {
    return this.http.get<PokemonListItem[]>(`${this.apiUrl}/pokemon`);
  }

  getPokemonDetails(idOrName: number | string): Observable<PokemonDetails> {
    return this.http.get<PokemonDetails>(`${this.apiUrl}/pokemon/${idOrName}`);
  }

  searchPokemons(query: string): Observable<PokemonListItem[]> {
    return this.http.get<PokemonListItem[]>(`${this.apiUrl}/pokemon/search?query=${encodeURIComponent(query)}`);
  }

  // 2. Team Endpoints
  getTeam(): Observable<DreamTeamMember[]> {
    return this.http.get<DreamTeamMember[]>(`${this.apiUrl}/team`);
  }

  addToTeam(pokemonId: number, slotIndex: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/team`, { pokemonId, slotIndex });
  }

  removeFromTeam(pokemonId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/team/pokemon/${pokemonId}`);
  }

  removeFromSlot(slotIndex: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/team/slot/${slotIndex}`);
  }

  clearTeam(): Observable<any> {
    return this.http.delete(`${this.apiUrl}/team/clear`);
  }

  // 3. AI Coach Endpoints
  getAiAnalysis(): Observable<AiCoachResponse> {
    return this.http.get<AiCoachResponse>(`${this.apiUrl}/aicoach/analyze`);
  }

  // Cache helpers
  private cacheSet(key: string, data: any, ttlMinutes: number = 60): void {
    const item = { data, expiry: Date.now() + ttlMinutes * 60 * 1000 };
    try { localStorage.setItem(key, JSON.stringify(item)); } catch {}
  }

  private cacheGet<T>(key: string): T | null {
    try {
      const raw = localStorage.getItem(key);
      if (!raw) return null;
      const item = JSON.parse(raw);
      if (item.expiry && Date.now() > item.expiry) {
        localStorage.removeItem(key);
        return null;
      }
      return item.data as T;
    } catch { return null; }
  }

  private cacheGetStale<T>(key: string): T | null {
    try {
      const raw = localStorage.getItem(key);
      if (!raw) return null;
      return JSON.parse(raw).data as T;
    } catch { return null; }
  }

  // Cached version of getPokemons
  getCachedPokemons(): Observable<PokemonListItem[]> {
    const cached = this.cacheGet<PokemonListItem[]>('pokemon_list_cache');
    return new Observable<PokemonListItem[]>(subscriber => {
      if (cached) subscriber.next(cached); // emit cached immediately
      this.getPokemons().subscribe({
        next: (data) => {
          this.cacheSet('pokemon_list_cache', data, 120); // 2 hour TTL
          subscriber.next(data);
          subscriber.complete();
        },
        error: (err) => {
          const stale = this.cacheGetStale<PokemonListItem[]>('pokemon_list_cache');
          if (stale && !cached) subscriber.next(stale);
          else if (!cached) {
            const fallback = this.getFallbackPokemons();
            this.cacheSet('pokemon_list_cache', fallback, 120);
            subscriber.next(fallback);
          }
          subscriber.complete();
        }
      });
    });
  }

  // Cached version of getPokemonDetails
  getCachedPokemonDetails(idOrName: string | number): Observable<PokemonDetails> {
    const cacheKey = `pokemon_detail_${idOrName}`;
    const cached = this.cacheGet<PokemonDetails>(cacheKey);
    if (cached) return new Observable(s => { s.next(cached); s.complete(); });
    return this.getPokemonDetails(idOrName).pipe(
      tap(data => this.cacheSet(cacheKey, data, 60))
    );
  }

  // Cached team operations
  getCachedTeam(username: string): Observable<DreamTeamMember[]> {
    const cacheKey = `team_cache_${username}`;
    const cached = this.cacheGet<DreamTeamMember[]>(cacheKey);
    return new Observable<DreamTeamMember[]>(subscriber => {
      if (cached) subscriber.next(cached);
      this.getTeam().subscribe({
        next: (data) => {
          this.cacheSet(cacheKey, data, 1440); // 24 hour TTL
          subscriber.next(data);
          subscriber.complete();
        },
        error: (err) => {
          const stale = this.cacheGetStale<DreamTeamMember[]>(cacheKey);
          if (stale && !cached) subscriber.next(stale);
          else if (!cached) subscriber.error(err);
          subscriber.complete();
        }
      });
    });
  }

  // Save team to cache (for optimistic updates)
  saveTeamToCache(username: string, team: DreamTeamMember[]): void {
    this.cacheSet(`team_cache_${username}`, team, 1440);
  }

  // Clear all user caches
  clearUserCaches(username: string): void {
    localStorage.removeItem(`team_cache_${username}`);
    // Pokemon list/detail caches are shared, don't clear them
  }

  // Frontend Fallbacks for Offline Resilience
  private fallbackNames = [
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
  ];

  private getFallbackTypes(id: number, name: string): { type1: string, type2?: string } {
    let type1 = 'normal';
    let type2: string | undefined = undefined;

    if (name === "bulbasaur" || name === "ivysaur" || name === "venusaur") { type1 = "grass"; type2 = "poison"; }
    else if (name === "charmander" || name === "charmeleon" || name === "charizard") { type1 = "fire"; if (name === "charizard") type2 = "flying"; }
    else if (name === "squirtle" || name === "wartortle" || name === "blastoise") { type1 = "water"; }
    else if (name === "caterpie" || name === "metapod" || name === "butterfree") { type1 = "bug"; if (name === "butterfree") type2 = "flying"; }
    else if (name === "weedle" || name === "kakuna" || name === "beedrill") { type1 = "bug"; type2 = "poison"; }
    else if (name === "pidgey" || name === "pidgeotto" || name === "pidgeot") { type1 = "normal"; type2 = "flying"; }
    else if (name === "rattata" || name === "raticate") { type1 = "normal"; }
    else if (name === "spearow" || name === "fearow") { type1 = "normal"; type2 = "flying"; }
    else if (name === "ekans" || name === "arbok") { type1 = "poison"; }
    else if (name === "pikachu" || name === "raichu") { type1 = "electric"; }
    else if (name === "sandshrew" || name === "sandslash") { type1 = "ground"; }
    else if (name === "nidoran-f" || name === "nidorina" || name === "nidoqueen") { type1 = "poison"; if (name === "nidoqueen") type2 = "ground"; }
    else if (name === "nidoran-m" || name === "nidorino" || name === "nidoking") { type1 = "poison"; if (name === "nidoking") type2 = "ground"; }
    else if (name === "clefairy" || name === "clefable") { type1 = "fairy"; }
    else if (name === "vulpix" || name === "ninetales") { type1 = "fire"; }
    else if (name === "jigglypuff" || name === "wigglytuff") { type1 = "normal"; type2 = "fairy"; }
    else if (name === "zubat" || name === "golbat") { type1 = "poison"; type2 = "flying"; }
    else if (name === "oddish" || name === "gloom" || name === "vileplume") { type1 = "grass"; type2 = "poison"; }
    else if (name === "paras" || name === "parasect") { type1 = "bug"; type2 = "grass"; }
    else if (name === "venonat" || name === "venomoth") { type1 = "bug"; type2 = "poison"; }
    else if (name === "diglett" || name === "dugtrio") { type1 = "ground"; }
    else if (name === "meowth" || name === "persian") { type1 = "normal"; }
    else if (name === "psyduck" || name === "golduck") { type1 = "water"; }
    else if (name === "mankey" || name === "primeape") { type1 = "fighting"; }
    else if (name === "growlithe" || name === "arcanine") { type1 = "fire"; }
    else if (name === "poliwag" || name === "poliwhirl" || name === "poliwrath") { type1 = "water"; if (name === "poliwrath") type2 = "fighting"; }
    else if (name === "abra" || name === "kadabra" || name === "alakazam") { type1 = "psychic"; }
    else if (name === "machop" || name === "machoke" || name === "machamp") { type1 = "fighting"; }
    else if (name === "bellsprout" || name === "weepinbell" || name === "victreebel") { type1 = "grass"; type2 = "poison"; }
    else if (name === "tentacool" || name === "tentacruel") { type1 = "water"; type2 = "poison"; }
    else if (name === "geodude" || name === "graveler" || name === "golem") { type1 = "rock"; type2 = "ground"; }
    else if (name === "ponyta" || name === "rapidash") { type1 = "fire"; }
    else if (name === "slowpoke" || name === "slowbro") { type1 = "water"; type2 = "psychic"; }
    else if (name === "magnemite" || name === "magneton") { type1 = "electric"; type2 = "steel"; }
    else if (name === "farfetchd") { type1 = "normal"; type2 = "flying"; }
    else if (name === "doduo" || name === "dodrio") { type1 = "normal"; type2 = "flying"; }
    else if (name === "seel" || name === "dewgong") { type1 = "water"; if (name === "dewgong") type2 = "ice"; }
    else if (name === "grimer" || name === "muk") { type1 = "poison"; }
    else if (name === "shellder" || name === "cloyster") { type1 = "water"; if (name === "cloyster") type2 = "ice"; }
    else if (name === "gastly" || name === "haunter" || name === "gengar") { type1 = "ghost"; type2 = "poison"; }
    else if (name === "onix") { type1 = "rock"; type2 = "ground"; }
    else if (name === "drowzee" || name === "hypno") { type1 = "psychic"; }
    else if (name === "krabby" || name === "kingler") { type1 = "water"; }
    else if (name === "voltorb" || name === "electrode") { type1 = "electric"; }
    else if (name === "exeggcute" || name === "exeggutor") { type1 = "grass"; type2 = "psychic"; }
    else if (name === "cubone" || name === "marowak") { type1 = "ground"; }
    else if (name === "hitmonlee") { type1 = "fighting"; }
    else if (name === "hitmonchan") { type1 = "fighting"; }
    else if (name === "lickitung") { type1 = "normal"; }
    else if (name === "koffing" || name === "weezing") { type1 = "poison"; }
    else if (name === "rhyhorn" || name === "rhydon") { type1 = "ground"; type2 = "rock"; }
    else if (name === "chansey") { type1 = "normal"; }
    else if (name === "tangela") { type1 = "grass"; }
    else if (name === "kangaskhan") { type1 = "normal"; }
    else if (name === "horsea" || name === "seadra") { type1 = "water"; }
    else if (name === "goldeen" || name === "seaking") { type1 = "water"; }
    else if (name === "staryu" || name === "starmie") { type1 = "water"; type2 = "psychic"; }
    else if (name === "mr-mime") { type1 = "psychic"; type2 = "fairy"; }
    else if (name === "scyther") { type1 = "bug"; type2 = "flying"; }
    else if (name === "jynx") { type1 = "ice"; type2 = "psychic"; }
    else if (name === "electabuzz") { type1 = "electric"; }
    else if (name === "magmar") { type1 = "fire"; }
    else if (name === "pinsir") { type1 = "bug"; }
    else if (name === "tauros") { type1 = "normal"; }
    else if (name === "magikarp" || name === "gyarados") { type1 = "water"; if (name === "gyarados") type2 = "flying"; }
    else if (name === "lapras") { type1 = "water"; type2 = "ice"; }
    else if (name === "ditto") { type1 = "normal"; }
    else if (name === "eevee" || name === "vaporeon" || name === "jolteon" || name === "flareon") {
      if (name === "eevee") type1 = "normal";
      else if (name === "vaporeon") type1 = "water";
      else if (name === "jolteon") type1 = "electric";
      else if (name === "flareon") type1 = "fire";
    }
    else if (name === "porygon") { type1 = "normal"; }
    else if (name === "omanyte" || name === "omastar") { type1 = "rock"; type2 = "water"; }
    else if (name === "kabuto" || name === "kabutops") { type1 = "rock"; type2 = "water"; }
    else if (name === "aerodactyl") { type1 = "rock"; type2 = "flying"; }
    else if (name === "snorlax") { type1 = "normal"; }
    else if (name === "articuno") { type1 = "ice"; type2 = "flying"; }
    else if (name === "zapdos") { type1 = "electric"; type2 = "flying"; }
    else if (name === "moltres") { type1 = "fire"; type2 = "flying"; }
    else if (name === "dratini" || name === "dragonair" || name === "dragonite") { type1 = "dragon"; if (name === "dragonite") type2 = "flying"; }
    else if (name === "mewtwo") { type1 = "psychic"; }
    else if (name === "mew") { type1 = "psychic"; }

    return { type1, type2 };
  }

  getFallbackPokemons(): PokemonListItem[] {
    return this.fallbackNames.map((name, index) => {
      const id = index + 1;
      const types = this.getFallbackTypes(id, name);
      return {
        id,
        name: name.charAt(0).toUpperCase() + name.substring(1),
        spriteUrl: `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/${id}.png`,
        type1: types.type1,
        type2: types.type2
      };
    });
  }
}
