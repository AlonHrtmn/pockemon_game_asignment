import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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
  private apiUrl = 'http://localhost:5072/api';

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
}
