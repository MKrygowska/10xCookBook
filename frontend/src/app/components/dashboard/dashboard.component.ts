import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { RecipeService, Ingredient, RecipeMatch } from '../../services/recipe.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  userEmail: string | null = null;
  
  allIngredients: Ingredient[] = [];
  availableIngredients: Ingredient[] = [];
  filteredIngredients: Ingredient[] = [];
  selectedIngredients: string[] = [];
  searchQuery: string = '';
  showDropdown: boolean = false;
  
  matchedRecipes: RecipeMatch[] = [];
  expandedRecipeId: string | null = null;
  isLoading: boolean = false;
  hasSearched: boolean = false;

  constructor(
    private authService: AuthService,
    private recipeService: RecipeService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userEmail = this.authService.getCurrentUserEmail();
    this.loadIngredients();
  }

  loadIngredients(): void {
    this.recipeService.getIngredients().pipe(takeUntil(this.destroy$)).subscribe({
      next: (ingredients) => {
        this.allIngredients = ingredients;
        this.updateAvailableIngredients();
      },
      error: (err) => {
        console.error('Failed to load ingredients:', err);
      }
    });
  }

  updateAvailableIngredients(): void {
    this.availableIngredients = this.allIngredients.filter(
      i => !this.selectedIngredients.some(sel => sel.toLowerCase() === i.name.toLowerCase())
    );
    this.filterIngredients();
  }

  filterIngredients(): void {
    const query = this.searchQuery.trim().toLowerCase();
    if (!query) {
      this.filteredIngredients = this.availableIngredients;
    } else {
      this.filteredIngredients = this.availableIngredients.filter(
        i => i.name.toLowerCase().includes(query)
      );
    }
  }

  onSearchQueryChange(): void {
    this.filterIngredients();
    this.showDropdown = true;
  }

  addIngredient(name: string): void {
    if (!name) return;
    const normalized = name.trim();
    if (normalized && !this.selectedIngredients.some(sel => sel.toLowerCase() === normalized.toLowerCase())) {
      this.selectedIngredients.push(normalized);
      this.searchQuery = '';
      this.showDropdown = false;
      this.updateAvailableIngredients();
      this.searchRecipes();
    }
  }

  removeIngredient(name: string): void {
    this.selectedIngredients = this.selectedIngredients.filter(i => i !== name);
    this.updateAvailableIngredients();
    if (this.selectedIngredients.length > 0) {
      this.searchRecipes();
    } else {
      this.matchedRecipes = [];
      this.hasSearched = false;
    }
  }

  onInputFocus(): void {
    this.filterIngredients();
    this.showDropdown = true;
  }

  onInputBlur(): void {
    // Delay hiding dropdown so that clicks on dropdown items can register first
    setTimeout(() => {
      this.showDropdown = false;
    }, 200);
  }

  onInputEnter(event: Event): void {
    event.preventDefault();
    const query = this.searchQuery.trim().toLowerCase();
    if (!query) return;

    const exactMatch = this.availableIngredients.find(
      i => i.name.toLowerCase() === query
    );

    if (exactMatch) {
      this.addIngredient(exactMatch.name);
    } else if (this.filteredIngredients.length > 0) {
      this.addIngredient(this.filteredIngredients[0].name);
    }
  }

  searchRecipes(): void {
    if (this.selectedIngredients.length === 0) {
      this.matchedRecipes = [];
      this.hasSearched = false;
      return;
    }

    this.isLoading = true;
    this.recipeService.matchRecipes(this.selectedIngredients).pipe(takeUntil(this.destroy$)).subscribe({
      next: (recipes) => {
        this.matchedRecipes = recipes;
        this.isLoading = false;
        this.hasSearched = true;
      },
      error: (err) => {
        console.error('Failed to match recipes:', err);
        this.isLoading = false;
      }
    });
  }

  toggleRecipeExpand(recipeId: string): void {
    if (this.expandedRecipeId === recipeId) {
      this.expandedRecipeId = null;
    } else {
      this.expandedRecipeId = recipeId;
    }
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
