import { Component, OnInit, OnDestroy, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { PokemonService, PokemonListItem, PokemonDetails, DreamTeamMember, AiCoachResponse } from '../../services/pokemon.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
  trainerName = '';
  pokemons: PokemonListItem[] = [];
  filteredPokemons: PokemonListItem[] = [];
  searchQuery = '';
  selectedType = '';
  
  // Dream Team slots (index 0 to 4). Using an array of 5 elements to represent the slots.
  teamSlots: (DreamTeamMember | null)[] = [null, null, null, null, null];
  selectedSlotIndex = 0; // default slot to place next pokemon

  // AI Coach drawer state
  showAiCoach = false;
  aiAnalysis: AiCoachResponse | null = null;
  aiLoading = false;

  // Pokemon details modal state
  selectedPokemon: PokemonDetails | null = null;
  detailsLoading = false;
  showDetailsModal = false;

  // DB Offline or API Down states
  dbOffline = false;
  apiDown = false;

  constructor(
    private authService: AuthService,
    private pokemonService: PokemonService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.trainerName = this.authService.currentUser() || 'Trainer';
  }

  ngOnInit(): void {
    this.loadPokemons();
    this.loadTeam();
  }

  loadPokemons(): void {
    this.pokemonService.getPokemons().subscribe({
      next: (data) => {
        this.pokemons = data;
        this.applyFilters();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.apiDown = true;
        console.error('Failed to load pokemons', err);
        this.cdr.markForCheck();
      }
    });
  }

  loadTeam(): void {
    this.pokemonService.getTeam().subscribe({
      next: (data) => {
        // Reset slots
        this.teamSlots = [null, null, null, null, null];
        
        // Fill slots based on backend slotIndex
        data.forEach(member => {
          if (member.slotIndex >= 0 && member.slotIndex < 5) {
            this.teamSlots[member.slotIndex] = member;
          }
        });
        
        this.dbOffline = false;

        // Auto-select next empty slot
        const nextEmpty = this.teamSlots.findIndex(s => s === null);
        if (nextEmpty !== -1) {
          this.selectedSlotIndex = nextEmpty;
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.dbOffline = true;
        console.error('Failed to load team', err);
        this.cdr.markForCheck();
      }
    });
  }

  onSearch(): void {
    this.applyFilters();
  }

  selectType(type: string): void {
    this.selectedType = this.selectedType === type ? '' : type;
    this.applyFilters();
  }

  applyFilters(): void {
    let list = this.pokemons;

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.trim().toLowerCase();
      list = list.filter(p => p.name.toLowerCase().includes(q) || p.id.toString() === q);
    }

    if (this.selectedType) {
      const t = this.selectedType.toLowerCase();
      list = list.filter(p => p.type1 === t || p.type2 === t);
    }

    this.filteredPokemons = list;
  }

  selectSlot(index: number): void {
    this.selectedSlotIndex = index;
  }

  addToTeam(pokemon: PokemonListItem): void {
    if (this.dbOffline) {
      alert('Database is offline. Changes to the team cannot be saved right now.');
      return;
    }

    const previousSlotState = this.teamSlots[this.selectedSlotIndex];
    const targetSlotIndex = this.selectedSlotIndex;

    // Optimistically update the slot in the UI immediately
    this.teamSlots[targetSlotIndex] = {
      id: 0,
      userId: 0,
      pokemonId: pokemon.id,
      pokemonName: pokemon.name,
      spriteUrl: pokemon.spriteUrl,
      type1: pokemon.type1,
      type2: pokemon.type2,
      slotIndex: targetSlotIndex,
      addedAt: new Date().toISOString()
    };
    this.cdr.markForCheck();

    // Call service to add Pokemon to the backend
    this.pokemonService.addToTeam(pokemon.id, targetSlotIndex).subscribe({
      next: () => {
        this.loadTeam(); // Reload team members for final sync from DB
        if (this.showAiCoach) {
          this.consultAiCoach(); // Refresh AI analysis if drawer is open
        }
      },
      error: (err) => {
        // Rollback on error
        this.teamSlots[targetSlotIndex] = previousSlotState;
        this.cdr.markForCheck();
        alert(err.error?.message || 'Failed to add pokemon to team.');
      }
    });
  }

  removeFromSlot(slotIndex: number, event: MouseEvent): void {
    event.stopPropagation(); // Prevent selecting the slot on remove button click
    if (this.dbOffline) {
      alert('Database is offline. Changes to the team cannot be saved right now.');
      return;
    }

    const previousSlotState = this.teamSlots[slotIndex];
    
    // Optimistically remove the slot in the UI immediately
    this.teamSlots[slotIndex] = null;
    this.cdr.markForCheck();

    this.pokemonService.removeFromSlot(slotIndex).subscribe({
      next: () => {
        this.loadTeam();
        if (this.showAiCoach) {
          this.consultAiCoach();
        }
      },
      error: (err) => {
        // Rollback on error
        this.teamSlots[slotIndex] = previousSlotState;
        this.cdr.markForCheck();
        alert(err.error?.message || 'Failed to remove pokemon from slot.');
      }
    });
  }

  viewPokemonDetails(pokemonId: number): void {
    this.detailsLoading = true;
    this.showDetailsModal = true;
    this.pokemonService.getPokemonDetails(pokemonId).subscribe({
      next: (details) => {
        this.selectedPokemon = details;
        this.detailsLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.detailsLoading = false;
        alert('Failed to retrieve Pokemon details.');
        this.showDetailsModal = false;
        this.cdr.markForCheck();
      }
    });
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedPokemon = null;
  }

  consultAiCoach(): void {
    this.showAiCoach = true;
    this.aiLoading = true;
    this.pokemonService.getAiAnalysis().subscribe({
      next: (res) => {
        this.aiAnalysis = res;
        this.aiLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.aiLoading = false;
        alert('Failed to get AI Coach feedback.');
        this.showAiCoach = false;
        this.cdr.markForCheck();
      }
    });
  }

  closeAiCoach(): void {
    this.showAiCoach = false;
    this.aiAnalysis = null;
  }

  // Scroll functionality
  private scrollInterval: any = null;
  private scrollUpInterval: any = null;

  ngOnDestroy(): void {
    this.stopScrollDown();
    this.stopScrollUp();
  }

  scrollToBottom(): void {
    window.scrollTo({
      top: document.body.scrollHeight,
      behavior: 'smooth'
    });
  }

  scrollToTop(): void {
    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  startScrollDown(event?: Event): void {
    if (event) {
      event.preventDefault(); // Prevent zoom/touch events on mobile
    }
    if (this.scrollInterval) return;

    this.scrollInterval = setInterval(() => {
      window.scrollBy({
        top: 25,
        behavior: 'smooth'
      });
    }, 20);
  }

  stopScrollDown(): void {
    if (this.scrollInterval) {
      clearInterval(this.scrollInterval);
      this.scrollInterval = null;
    }
  }

  startScrollUp(event?: Event): void {
    if (event) {
      event.preventDefault(); // Prevent zoom/touch events on mobile
    }
    if (this.scrollUpInterval) return;

    this.scrollUpInterval = setInterval(() => {
      window.scrollBy({
        top: -25,
        behavior: 'smooth'
      });
    }, 20);
  }

  stopScrollUp(): void {
    if (this.scrollUpInterval) {
      clearInterval(this.scrollUpInterval);
      this.scrollUpInterval = null;
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth']);
  }
}
