import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Ingredient {
  id: string;
  name: string;
  isSpiceOrStaple: boolean;
}

export interface MissingIngredient {
  name: string;
  isSpiceOrStaple: boolean;
  quantity: string;
}

export interface RecipeMatch {
  id: string;
  title: string;
  instructions: string;
  matchRate: number;
  matchedIngredients: string[];
  missingIngredients: MissingIngredient[];
}

export interface RecipeIngredientDto {
  ingredientId: string;
  name?: string; // Opcjonalna nazwa pobierana pomocniczo z autocomplete
  quantity: string;
}

export interface Recipe {
  id?: string;
  title: string;
  instructions: string;
  isPublic: boolean;
  ingredients: RecipeIngredientDto[];
}

@Injectable({
  providedIn: 'root'
})
export class RecipeService {
  private readonly apiUrl = window.location.hostname === 'localhost' 
    ? 'http://localhost:5174/api' 
    : '/api';

  constructor(private http: HttpClient) {}

  getIngredients(): Observable<Ingredient[]> {
    return this.http.get<Ingredient[]>(`${this.apiUrl}/ingredients`);
  }

  matchRecipes(ingredients: string[]): Observable<RecipeMatch[]> {
    return this.http.post<RecipeMatch[]>(`${this.apiUrl}/recipes/match`, { ingredients });
  }

  getUserRecipes(): Observable<Recipe[]> {
    return this.http.get<Recipe[]>(`${this.apiUrl}/recipes/my`);
  }

  createRecipe(recipe: Recipe): Observable<Recipe> {
    return this.http.post<Recipe>(`${this.apiUrl}/recipes`, recipe);
  }

  updateRecipe(id: string, recipe: Recipe): Observable<Recipe> {
    return this.http.put<Recipe>(`${this.apiUrl}/recipes/${id}`, recipe);
  }

  deleteRecipe(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/recipes/${id}`);
  }
}
