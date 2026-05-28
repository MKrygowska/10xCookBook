import { TestBed, ComponentFixture, fakeAsync, tick } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';
import { AuthService } from '../../services/auth.service';
import { RecipeService, Ingredient, RecipeMatch } from '../../services/recipe.service';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let recipeServiceSpy: jasmine.SpyObj<RecipeService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const mockIngredients: Ingredient[] = [
    { id: '1', name: 'pomidor', isSpiceOrStaple: false },
    { id: '2', name: 'cebula', isSpiceOrStaple: false },
    { id: '3', name: 'sól', isSpiceOrStaple: true }
  ];

  const mockRecipes: RecipeMatch[] = [
    {
      id: 'r1',
      title: 'Zupa pomidorowa',
      instructions: 'Gotuj...',
      matchRate: 91,
      matchedIngredients: ['pomidor'],
      missingIngredients: [{ name: 'sól', isSpiceOrStaple: true, quantity: 'szczypta' }]
    }
  ];

  beforeEach(async () => {
    const authSpy = jasmine.createSpyObj('AuthService', ['getCurrentUserEmail', 'logout']);
    const recipeSpy = jasmine.createSpyObj('RecipeService', ['getIngredients', 'matchRecipes']);
    const rotSpy = jasmine.createSpyObj('Router', ['navigate']);

    authSpy.getCurrentUserEmail.and.returnValue('user@example.com');
    recipeSpy.getIngredients.and.returnValue(of(mockIngredients));
    recipeSpy.matchRecipes.and.returnValue(of(mockRecipes));

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
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
    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should load user email and ingredients on init', () => {
    expect(authServiceSpy.getCurrentUserEmail).toHaveBeenCalled();
    expect(component.userEmail).toBe('user@example.com');
    expect(recipeServiceSpy.getIngredients).toHaveBeenCalled();
    expect(component.allIngredients).toEqual(mockIngredients);
    expect(component.availableIngredients).toEqual(mockIngredients);
  });

  it('should filter available ingredients based on search query', () => {
    component.searchQuery = 'po';
    component.filterIngredients();
    expect(component.filteredIngredients.length).toBe(1);
    expect(component.filteredIngredients[0].name).toBe('pomidor');

    component.searchQuery = '   ';
    component.filterIngredients();
    expect(component.filteredIngredients).toEqual(component.availableIngredients);
  });

  it('should add ingredient when addIngredient is called', () => {
    component.addIngredient('pomidor');
    expect(component.selectedIngredients).toContain('pomidor');
    expect(component.searchQuery).toBe('');
    expect(component.showDropdown).toBeFalse();
    expect(component.availableIngredients.some(i => i.name === 'pomidor')).toBeFalse();
    expect(recipeServiceSpy.matchRecipes).toHaveBeenCalledWith(['pomidor']);
    expect(component.matchedRecipes).toEqual(mockRecipes);
  });

  it('should not add duplicate ingredient (exact case)', () => {
    component.addIngredient('pomidor');
    recipeServiceSpy.matchRecipes.calls.reset();
    component.addIngredient('pomidor');
    expect(component.selectedIngredients.length).toBe(1);
    expect(recipeServiceSpy.matchRecipes).not.toHaveBeenCalled();
  });

  it('should not add duplicate ingredients with different casing', () => {
    component.addIngredient('pomidor');
    recipeServiceSpy.matchRecipes.calls.reset();
    component.addIngredient('Pomidor');
    expect(component.selectedIngredients.length).toBe(1);
    expect(component.selectedIngredients[0]).toBe('pomidor');
    expect(recipeServiceSpy.matchRecipes).not.toHaveBeenCalled();
  });

  it('should remove ingredient when removeIngredient is called', () => {
    component.addIngredient('pomidor');
    component.addIngredient('cebula');
    recipeServiceSpy.matchRecipes.calls.reset();

    component.removeIngredient('pomidor');
    expect(component.selectedIngredients).not.toContain('pomidor');
    expect(component.selectedIngredients).toContain('cebula');
    expect(component.availableIngredients.some(i => i.name === 'pomidor')).toBeTrue();
    expect(recipeServiceSpy.matchRecipes).toHaveBeenCalledWith(['cebula']);
  });

  it('should clear matches when last ingredient is removed', () => {
    component.addIngredient('pomidor');
    component.removeIngredient('pomidor');
    expect(component.selectedIngredients.length).toBe(0);
    expect(component.matchedRecipes.length).toBe(0);
    expect(component.hasSearched).toBeFalse();
  });

  it('should handle Enter key with exact match', () => {
    component.searchQuery = 'pomidor';
    const event = new Event('keydown.enter');
    spyOn(event, 'preventDefault');

    component.onInputEnter(event);
    expect(event.preventDefault).toHaveBeenCalled();
    expect(component.selectedIngredients).toContain('pomidor');
  });

  it('should handle Enter key with partial match (first option)', () => {
    component.searchQuery = 'po';
    component.filterIngredients();
    const event = new Event('keydown.enter');
    spyOn(event, 'preventDefault');

    component.onInputEnter(event);
    expect(event.preventDefault).toHaveBeenCalled();
    expect(component.selectedIngredients).toContain('pomidor');
  });

  it('should toggle recipe expand details', () => {
    expect(component.expandedRecipeId).toBeNull();
    component.toggleRecipeExpand('r1');
    expect(component.expandedRecipeId).toBe('r1');
    component.toggleRecipeExpand('r1');
    expect(component.expandedRecipeId).toBeNull();
  });

  it('should navigate to login on logout', () => {
    component.onLogout();
    expect(authServiceSpy.logout).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should delay hiding dropdown on input blur', fakeAsync(() => {
    component.showDropdown = true;
    component.onInputBlur();
    expect(component.showDropdown).toBeTrue();
    tick(200);
    expect(component.showDropdown).toBeFalse();
  }));
});
