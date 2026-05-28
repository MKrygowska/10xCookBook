import { TestBed, ComponentFixture, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { MyRecipesComponent } from './my-recipes.component';
import { AuthService } from '../../services/auth.service';
import { RecipeService, Recipe, Ingredient } from '../../services/recipe.service';

describe('MyRecipesComponent', () => {
  let component: MyRecipesComponent;
  let fixture: ComponentFixture<MyRecipesComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let recipeServiceSpy: jasmine.SpyObj<RecipeService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const mockIngredients: Ingredient[] = [
    { id: '1', name: 'pomidor', isSpiceOrStaple: false },
    { id: '2', name: 'cebula', isSpiceOrStaple: false },
    { id: '3', name: 'sól', isSpiceOrStaple: true }
  ];

  const mockRecipes: Recipe[] = [
    {
      id: 'r1',
      title: 'Mój Omlet',
      instructions: 'Usmaż...',
      isPublic: false,
      ingredients: [
        { ingredientId: '1', name: 'pomidor', quantity: '1 sztuka' }
      ]
    }
  ];

  beforeEach(async () => {
    const authSpy = jasmine.createSpyObj('AuthService', ['getCurrentUserEmail', 'logout']);
    const recipeSpy = jasmine.createSpyObj('RecipeService', [
      'getUserRecipes', 
      'getIngredients', 
      'createRecipe', 
      'updateRecipe', 
      'deleteRecipe'
    ]);
    const rotSpy = jasmine.createSpyObj('Router', ['navigate']);

    authSpy.getCurrentUserEmail.and.returnValue('user@example.com');
    recipeSpy.getUserRecipes.and.returnValue(of(mockRecipes));
    recipeSpy.getIngredients.and.returnValue(of(mockIngredients));
    recipeSpy.createRecipe.and.returnValue(of(mockRecipes[0]));
    recipeSpy.updateRecipe.and.returnValue(of(mockRecipes[0]));
    recipeSpy.deleteRecipe.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [MyRecipesComponent, ReactiveFormsModule, FormsModule],
      providers: [
        { provide: AuthService, useValue: authSpy },
        { provide: RecipeService, useValue: recipeSpy },
        { provide: Router, useValue: rotSpy },
        { provide: ActivatedRoute, useValue: {} }
      ]
    }).compileComponents();

    authServiceSpy = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    recipeServiceSpy = TestBed.inject(RecipeService) as jasmine.SpyObj<RecipeService>;
    routerSpy = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(MyRecipesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should load user email, recipes, and ingredients on init', () => {
    expect(authServiceSpy.getCurrentUserEmail).toHaveBeenCalled();
    expect(component.userEmail).toBe('user@example.com');
    expect(recipeServiceSpy.getUserRecipes).toHaveBeenCalled();
    expect(component.recipes).toEqual(mockRecipes);
    expect(recipeServiceSpy.getIngredients).toHaveBeenCalled();
    expect(component.allIngredients).toEqual(mockIngredients);
  });

  it('should open modal to add new recipe with empty form', () => {
    component.openAddModal();
    expect(component.showFormModal).toBeTrue();
    expect(component.selectedRecipe).toBeNull();
    expect(component.recipeForm.get('title')?.value).toBe('');
    expect(component.recipeForm.get('instructions')?.value).toBe('');
    expect(component.ingredientsFormArray.length).toBe(0);
  });

  it('should open modal to edit existing recipe and populate form fields', () => {
    component.openEditModal(mockRecipes[0]);
    expect(component.showFormModal).toBeTrue();
    expect(component.selectedRecipe).toEqual(mockRecipes[0]);
    expect(component.recipeForm.get('title')?.value).toBe('Mój Omlet');
    expect(component.recipeForm.get('instructions')?.value).toBe('Usmaż...');
    expect(component.ingredientsFormArray.length).toBe(1);
    expect(component.ingredientsFormArray.at(0).get('ingredientId')?.value).toBe('1');
    expect(component.ingredientsFormArray.at(0).get('name')?.value).toBe('pomidor');
    expect(component.ingredientsFormArray.at(0).get('quantity')?.value).toBe('1 sztuka');
  });

  it('should open delete confirmation modal', () => {
    component.openDeleteModal(mockRecipes[0]);
    expect(component.showDeleteModal).toBeTrue();
    expect(component.selectedRecipe).toEqual(mockRecipes[0]);
  });

  it('should add ingredient to FormArray and filter available ingredients', () => {
    component.openAddModal();
    const ingToAdd = mockIngredients[0]; // pomidor
    component.selectIngredient(ingToAdd);
    
    expect(component.ingredientsFormArray.length).toBe(1);
    expect(component.ingredientsFormArray.at(0).get('ingredientId')?.value).toBe('1');
    expect(component.ingredientsFormArray.at(0).get('name')?.value).toBe('pomidor');
    
    const available = component.getAvailableIngredients();
    expect(available.some(i => i.id === '1')).toBeFalse();
    expect(available.some(i => i.id === '2')).toBeTrue();
  });

  it('should remove ingredient from FormArray', () => {
    component.openEditModal(mockRecipes[0]);
    expect(component.ingredientsFormArray.length).toBe(1);
    
    component.removeIngredientFromForm(0);
    expect(component.ingredientsFormArray.length).toBe(0);
  });

  it('should validate form and show error if submitted empty', () => {
    component.openAddModal();
    component.onSubmit();
    
    expect(component.recipeForm.valid).toBeFalse();
    expect(component.isFieldInvalid('title')).toBeTrue();
    expect(component.isFieldInvalid('instructions')).toBeTrue();
  });

  it('should call createRecipe service when submitting new recipe', () => {
    component.openAddModal();
    component.recipeForm.get('title')?.setValue('Nowy przepis');
    component.recipeForm.get('instructions')?.setValue('Instrukcje gotowania');
    component.addIngredientToForm('1', 'pomidor', '2 sztuki');
    
    component.onSubmit();
    expect(recipeServiceSpy.createRecipe).toHaveBeenCalled();
    expect(component.showFormModal).toBeFalse();
    expect(recipeServiceSpy.getUserRecipes).toHaveBeenCalledTimes(2); // Initial + on success load
  });

  it('should call updateRecipe service when submitting edited recipe', () => {
    component.openEditModal(mockRecipes[0]);
    component.recipeForm.get('title')?.setValue('Zaktualizowany Omlet');
    
    component.onSubmit();
    expect(recipeServiceSpy.updateRecipe).toHaveBeenCalledWith('r1', jasmine.any(Object));
    expect(component.showFormModal).toBeFalse();
    expect(recipeServiceSpy.getUserRecipes).toHaveBeenCalledTimes(2);
  });

  it('should call deleteRecipe service when confirming delete', () => {
    component.openDeleteModal(mockRecipes[0]);
    component.onDeleteConfirm();
    
    expect(recipeServiceSpy.deleteRecipe).toHaveBeenCalledWith('r1');
    expect(component.showDeleteModal).toBeFalse();
    expect(recipeServiceSpy.getUserRecipes).toHaveBeenCalledTimes(2);
  });

  it('should delay hiding ingredient dropdown on blur', fakeAsync(() => {
    component.showIngredientDropdown = true;
    component.onIngredientBlur();
    expect(component.showIngredientDropdown).toBeTrue();
    tick(200);
    expect(component.showIngredientDropdown).toBeFalse();
  }));
});
