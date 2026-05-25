import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="premium-card">
        <div class="brand-header">
          <div class="brand-logo">10xCookBook</div>
          <div class="brand-subtitle">Zaloguj się do swojego panelu</div>
        </div>

        <div *ngIf="errorMessage" class="alert-card">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
          <span>{{ errorMessage }}</span>
        </div>

        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label class="form-label" for="email">E-mail</label>
            <input 
              type="email" 
              id="email" 
              formControlName="email" 
              class="form-input" 
              placeholder="twoj@email.com"
              autocomplete="email"
            />
            <span *ngIf="isFieldInvalid('email')" class="validation-error">
              Wprowadź poprawny adres e-mail
            </span>
          </div>

          <div class="form-group">
            <label class="form-label" for="password">Hasło</label>
            <input 
              type="password" 
              id="password" 
              formControlName="password" 
              class="form-input" 
              placeholder="••••••••"
              autocomplete="current-password"
            />
            <span *ngIf="isFieldInvalid('password')" class="validation-error">
              Hasło jest wymagane
            </span>
          </div>

          <button type="submit" [disabled]="loginForm.invalid || isLoading" class="premium-btn">
            {{ isLoading ? 'Logowanie...' : 'Zaloguj się' }}
          </button>
        </form>

        <div class="auth-switch">
          Nie masz jeszcze konta? 
          <a class="auth-link" routerLink="/register">Zarejestruj się</a>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  isLoading = false;
  errorMessage: string | null = null;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // If already authenticated, redirect to dashboard
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
      return;
    }

    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        if (err.error && err.error.message) {
          this.errorMessage = err.error.message;
        } else {
          this.errorMessage = 'Nie udało się zalogować. Sprawdź połączenie z serwerem.';
        }
      }
    });
  }
}
