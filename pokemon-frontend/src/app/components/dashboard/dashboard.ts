import { Component, OnInit, OnDestroy, signal, ChangeDetectorRef, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
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
  @ViewChild('typeFiltersContainer') typeFiltersContainer!: ElementRef;
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
  isAiCoachMaximized = false;
  aiAnalysis: AiCoachResponse | null = null;
  aiLoading = false;

  // Pokemon details modal state
  selectedPokemon: PokemonDetails | null = null;
  detailsLoading = false;
  showDetailsModal = false;
  openedFromTeamSlotIndex: number | null = null;
  isRefreshing = false;

  // DB Offline or API Down states
  dbOffline = false;
  apiDown = false;
  teamUpdating = false;
  private detailsSubscription: Subscription | null = null;

  // Toast notifications
  toasts: { message: string; type: 'success' | 'error' | 'warning' | 'info'; id: number }[] = [];
  private toastId = 0;

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

  loadPokemons(forceRefresh: boolean = false): void {
    if (forceRefresh) {
      if (this.isRefreshing) {
        return; // Synchronous guard to prevent duplicate rapid trigger clicks
      }
      this.isRefreshing = true;
      this.searchQuery = '';
      this.selectedType = '';
      localStorage.removeItem('pokemon_list_cache');
      localStorage.removeItem('pokemon_list_cache_time');
      for (let i = localStorage.length - 1; i >= 0; i--) {
        const key = localStorage.key(i);
        if (key && key.startsWith('pokemon_detail_')) {
          localStorage.removeItem(key);
        }
      }
      this.loadTeam();
    }

    this.pokemonService.getCachedPokemons().subscribe({
      next: (data) => {
        this.pokemons = data;
        this.apiDown = false;
        this.applyFilters();
        this.isRefreshing = false;
        if (forceRefresh) {
          this.showToast('Pokemon Database refreshed successfully.', 'success');
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.apiDown = true;
        this.isRefreshing = false;
        console.error('Failed to load pokemons', err);
        if (forceRefresh) {
          this.showToast('Failed to refresh Pokemon Database.', 'error');
        }
        this.cdr.markForCheck();
      }
    });
  }

  loadTeam(): void {
    const username = this.authService.currentUser() || '';
    this.pokemonService.getCachedTeam(username).subscribe({
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
    if (this.teamUpdating || this.aiLoading) {
      return;
    }
    this.selectedType = this.selectedType === type ? '' : type;
    this.applyFilters();
  }

  scrollFilters(amount: number): void {
    if (this.typeFiltersContainer) {
      const el = this.typeFiltersContainer.nativeElement;
      el.scrollBy({ left: amount, behavior: 'smooth' });
    }
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
    if (this.teamUpdating || this.aiLoading) {
      return;
    }
    this.selectedSlotIndex = index;
  }

  addToTeam(pokemon: PokemonListItem): void {
    if (this.dbOffline) {
      this.showToast('Database is offline. Changes cannot be saved right now.', 'warning');
      return;
    }
    if (this.teamUpdating) {
      return; // Block concurrent operations
    }

    const targetSlotIndex = this.selectedSlotIndex;

    // If this Pokemon is already in the target slot, silently ignore
    const currentInSlot = this.teamSlots[targetSlotIndex];
    if (currentInSlot !== null && currentInSlot.pokemonId === pokemon.id) {
      return;
    }

    this.teamUpdating = true;
    const previousSlots = [...this.teamSlots]; // Snapshot entire team for rollback

    // If this Pokemon exists in another slot, clear it (optimistic move)
    for (let i = 0; i < this.teamSlots.length; i++) {
      if (this.teamSlots[i]?.pokemonId === pokemon.id) {
        this.teamSlots[i] = null;
      }
    }

    // Optimistically update the target slot in the UI immediately
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
    this.pokemonService.saveTeamToCache(this.authService.currentUser() || '', this.teamSlots.filter(s => s !== null) as DreamTeamMember[]);
    this.cdr.markForCheck();

    // Call service to add Pokemon to the backend
    this.pokemonService.addToTeam(pokemon.id, targetSlotIndex).subscribe({
      next: () => {
        // Keep teamUpdating=true until loadTeam finishes to prevent rapid clicks
        const username = this.authService.currentUser() || '';
        this.pokemonService.getCachedTeam(username).subscribe({
          next: (data) => {
            this.teamSlots = [null, null, null, null, null];
            data.forEach(member => {
              if (member.slotIndex >= 0 && member.slotIndex < 5) {
                this.teamSlots[member.slotIndex] = member;
              }
            });
            this.dbOffline = false;
            const nextEmpty = this.teamSlots.findIndex(s => s === null);
            if (nextEmpty !== -1) {
              this.selectedSlotIndex = nextEmpty;
            }
            this.teamUpdating = false;
            this.cdr.markForCheck();
          },
          error: () => {
            this.teamUpdating = false;
            this.cdr.markForCheck();
          }
        });
        if (this.showAiCoach) {
          this.consultAiCoach();
        }
      },
      error: (err) => {
        this.teamUpdating = false;
        // Rollback entire team state on error
        for (let i = 0; i < previousSlots.length; i++) {
          this.teamSlots[i] = previousSlots[i];
        }
        this.cdr.markForCheck();
        // Don't show error for duplicate/already-in-team — not a real user error
        const msg = err.error?.message || '';
        if (!msg.toLowerCase().includes('already') && !msg.toLowerCase().includes('duplicate')) {
          this.showToast(msg || 'Failed to add pokemon to team.', 'error');
        }
      }
    });
  }

  removeFromSlot(slotIndex: number, event?: MouseEvent): void {
    if (event) {
      event.stopPropagation(); // Prevent selecting the slot on remove button click
    }
    if (this.dbOffline) {
      this.showToast('Database is offline. Changes cannot be saved right now.', 'warning');
      return;
    }
    if (this.teamUpdating) {
      return; // Block concurrent operations
    }

    // If slot is already empty, silently ignore
    if (this.teamSlots[slotIndex] === null) {
      return;
    }

    this.teamUpdating = true;
    const previousSlotState = this.teamSlots[slotIndex];
    
    // Optimistically remove the slot in the UI immediately
    this.teamSlots[slotIndex] = null;
    this.pokemonService.saveTeamToCache(this.authService.currentUser() || '', this.teamSlots.filter(s => s !== null) as DreamTeamMember[]);
    this.cdr.markForCheck();

    this.pokemonService.removeFromSlot(slotIndex).subscribe({
      next: () => {
        // Keep teamUpdating=true until loadTeam finishes
        const username = this.authService.currentUser() || '';
        this.pokemonService.getCachedTeam(username).subscribe({
          next: (data) => {
            this.teamSlots = [null, null, null, null, null];
            data.forEach(member => {
              if (member.slotIndex >= 0 && member.slotIndex < 5) {
                this.teamSlots[member.slotIndex] = member;
              }
            });
            this.dbOffline = false;
            const nextEmpty = this.teamSlots.findIndex(s => s === null);
            if (nextEmpty !== -1) {
              this.selectedSlotIndex = nextEmpty;
            }
            this.teamUpdating = false;
            this.cdr.markForCheck();
          },
          error: () => {
            this.teamUpdating = false;
            this.cdr.markForCheck();
          }
        });
        if (this.showAiCoach) {
          this.consultAiCoach();
        }
      },
      error: (err) => {
        this.teamUpdating = false;
        // Rollback on error
        this.teamSlots[slotIndex] = previousSlotState;
        this.cdr.markForCheck();
        // Don't show error for 404 (slot already empty) — not a real user error
        if (err.status !== 404) {
          this.showToast(err.error?.message || 'Failed to remove pokemon from slot.', 'error');
        }
      }
    });
  }

  removeFromSlotAndClose(slotIndex: number): void {
    if (this.teamUpdating) {
      return;
    }
    this.removeFromSlot(slotIndex);
    this.closeDetailsModal();
  }

  viewPokemonDetails(pokemonId: number, slotIndex: number | null = null): void {
    if (this.detailsLoading) {
      return; // Block concurrent detail loading clicks
    }
    this.openedFromTeamSlotIndex = slotIndex;
    if (slotIndex !== null) {
      this.selectedSlotIndex = slotIndex;
    }
    this.detailsLoading = true;
    this.showDetailsModal = true;
    this.selectedPokemon = null; // Clear previous details immediately to prevent temporary stale rendering

    if (this.detailsSubscription) {
      this.detailsSubscription.unsubscribe();
    }

    this.detailsSubscription = this.pokemonService.getCachedPokemonDetails(pokemonId).subscribe({
      next: (details) => {
        this.selectedPokemon = details;
        this.detailsLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.detailsLoading = false;
        this.showToast('Failed to retrieve Pokemon details.', 'error');
        this.showDetailsModal = false;
        this.cdr.markForCheck();
      }
    });
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedPokemon = null;
    this.detailsLoading = false;
    this.openedFromTeamSlotIndex = null;
    if (this.detailsSubscription) {
      this.detailsSubscription.unsubscribe();
      this.detailsSubscription = null;
    }
  }

  consultAiCoach(): void {
    if (this.aiLoading) {
      return;
    }
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
        this.showToast('Failed to get AI Coach analysis.', 'error');
        this.showAiCoach = false;
        this.cdr.markForCheck();
      }
    });
  }

  toggleMaximizeAiCoach(): void {
    this.isAiCoachMaximized = !this.isAiCoachMaximized;
  }

  closeAiCoach(): void {
    this.showAiCoach = false;
    this.aiAnalysis = null;
    this.isAiCoachMaximized = false;
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

  showToast(message: string, type: 'success' | 'error' | 'warning' | 'info' = 'info', duration: number = 4000): void {
    this.toasts = []; // Ensure at most one toast is displayed at any given time
    const id = ++this.toastId;
    this.toasts.push({ message, type, id });
    setTimeout(() => this.dismissToast(id), duration);
  }

  dismissToast(id: number): void {
    this.toasts = this.toasts.filter(t => t.id !== id);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth']);
  }
}
