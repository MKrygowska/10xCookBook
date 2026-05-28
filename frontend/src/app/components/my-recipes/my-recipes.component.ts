import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { RecipeService, Recipe, Ingredient } from '../../services/recipe.service';

@Component({
  selector: 'app-my-recipes',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink],
  templateUrl: './my-recipes.component.html',
  styleUrls: ['./my-recipes.component.scss']
})
export class MyRecipesComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  userEmail: string | null = null;
  
  recipes: Recipe[] = [];
  allIngredients: Ingredient[] = [];
  filteredIngredients: Ingredient[] = [];
  
  showFormModal = false;
  showDeleteModal = false;
  isSaving = false;
  isLoading = false;
  errorMessage: string | null = null;
  
  selectedRecipe: Recipe | null = null;
  recipeForm!: FormGroup;
  
  // Autocomplete state inside form modal
  ingredientSearchQuery = '';
  showIngredientDropdown = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private recipeService: RecipeService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userEmail = this.authService.getCurrentUserEmail();
    this.loadRecipes();
    this.loadIngredients();
    this.initForm();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  initForm(): void {
    this.recipeForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(100)]],
      instructions: ['', [Validators.required]],
      ingredients: this.fb.array([])
    });
  }

  get ingredientsFormArray(): FormArray {
    return this.recipeForm.get('ingredients') as FormArray;
  }

  loadRecipes(): void {
    this.isLoading = true;
    this.recipeService.getUserRecipes()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (recipes) => {
          this.recipes = recipes;
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load recipes:', err);
          this.errorMessage = 'Nie udało się pobrać Twoich przepisów.';
          this.isLoading = false;
        }
      });
  }

  loadIngredients(): void {
    this.recipeService.getIngredients()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (ingredients) => {
          this.allIngredients = ingredients;
        },
        error: (err) => {
          console.error('Failed to load ingredients:', err);
        }
      });
  }

  openAddModal(): void {
    this.selectedRecipe = null;
    this.errorMessage = null;
    this.ingredientSearchQuery = '';
    this.showIngredientDropdown = false;
    this.initForm();
    this.showFormModal = true;
  }

  openEditModal(recipe: Recipe): void {
    this.selectedRecipe = recipe;
    this.errorMessage = null;
    this.ingredientSearchQuery = '';
    this.showIngredientDropdown = false;
    
    this.recipeForm = this.fb.group({
      title: [recipe.title, [Validators.required, Validators.maxLength(100)]],
      instructions: [recipe.instructions, [Validators.required]],
      ingredients: this.fb.array([])
    });

    if (recipe.ingredients) {
      recipe.ingredients.forEach(ing => {
        this.addIngredientToForm(ing.ingredientId, ing.name || '', ing.quantity);
      });
    }
    
    this.showFormModal = true;
  }

  openDeleteModal(recipe: Recipe): void {
    this.selectedRecipe = recipe;
    this.showDeleteModal = true;
  }

  closeFormModal(): void {
    this.showFormModal = false;
    this.selectedRecipe = null;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.selectedRecipe = null;
  }

  addIngredientToForm(id: string, name: string, quantity: string = ''): void {
    const group = this.fb.group({
      ingredientId: [id, Validators.required],
      name: [name], // Helper field to display the name
      quantity: [quantity, Validators.required]
    });
    this.ingredientsFormArray.push(group);
  }

  removeIngredientFromForm(index: number): void {
    this.ingredientsFormArray.removeAt(index);
  }

  getAvailableIngredients(): Ingredient[] {
    const selectedIds = this.ingredientsFormArray.value.map((val: any) => val.ingredientId);
    return this.allIngredients.filter(ing => !selectedIds.includes(ing.id));
  }

  filterIngredients(): void {
    const query = this.ingredientSearchQuery.trim().toLowerCase();
    const available = this.getAvailableIngredients();
    if (!query) {
      this.filteredIngredients = available;
    } else {
      this.filteredIngredients = available.filter(ing => 
        ing.name.toLowerCase().includes(query)
      );
    }
  }

  onIngredientSearchChange(): void {
    this.filterIngredients();
    this.showIngredientDropdown = true;
  }

  onIngredientFocus(): void {
    this.filterIngredients();
    this.showIngredientDropdown = true;
  }

  onIngredientBlur(): void {
    setTimeout(() => {
      this.showIngredientDropdown = false;
    }, 200);
  }

  selectIngredient(ing: Ingredient): void {
    this.addIngredientToForm(ing.id, ing.name);
    this.ingredientSearchQuery = '';
    this.showIngredientDropdown = false;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.recipeForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSubmit(): void {
    if (this.recipeForm.invalid) {
      this.recipeForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = null;

    // Build the recipe object conforming to the API DTO
    const formValue = this.recipeForm.value;
    const recipePayload: Recipe = {
      title: formValue.title.trim(),
      instructions: formValue.instructions.trim(),
      isPublic: false,
      ingredients: formValue.ingredients.map((ing: any) => ({
        ingredientId: ing.ingredientId,
        quantity: ing.quantity.trim()
      }))
    };

    if (this.selectedRecipe && this.selectedRecipe.id) {
      // Edit mode
      this.recipeService.updateRecipe(this.selectedRecipe.id, recipePayload)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.isSaving = false;
            this.closeFormModal();
            this.loadRecipes();
          },
          error: (err) => {
            console.error('Failed to update recipe:', err);
            this.errorMessage = err.error?.error || 'Nie udało się zaktualizować przepisu.';
            this.isSaving = false;
          }
        });
    } else {
      // Add mode
      this.recipeService.createRecipe(recipePayload)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.isSaving = false;
            this.closeFormModal();
            this.loadRecipes();
          },
          error: (err) => {
            console.error('Failed to create recipe:', err);
            this.errorMessage = err.error?.error || 'Nie udało się dodać przepisu.';
            this.isSaving = false;
          }
        });
    }
  }

  onDeleteConfirm(): void {
    if (!this.selectedRecipe || !this.selectedRecipe.id) return;

    this.recipeService.deleteRecipe(this.selectedRecipe.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.closeDeleteModal();
          this.loadRecipes();
        },
        error: (err) => {
          console.error('Failed to delete recipe:', err);
          this.errorMessage = 'Nie udało się usunąć przepisu.';
          this.closeDeleteModal();
        }
      });
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
