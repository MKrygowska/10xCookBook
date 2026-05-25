import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="auth-container" style="max-width: 600px;">
      <div class="premium-card dashboard-card">
        <div class="user-badge">
          Zalogowany jako: <strong>{{ userEmail }}</strong>
        </div>
        
        <h1 class="welcome-title">Witaj w 10xCookBook!</h1>
        
        <p class="dashboard-desc">
          Pomyślnie przeszedłeś autoryzację JWT. Twoja sesja jest bezpiecznie przechowywana w przeglądarce i automatycznie dołączana do każdego zapytania API. To jest Twój prywatny panel deweloperski, stanowiący pierwszy krok wdrożeniowy naszej bazy kodu.
        </p>

        <button (click)="onLogout()" class="premium-btn btn-secondary">
          Wyloguj się
        </button>
      </div>
    </div>
  `
})
export class DashboardComponent implements OnInit {
  userEmail: string | null = null;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userEmail = this.authService.getCurrentUserEmail();
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
