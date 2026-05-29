import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
  userEmail: string = '';
  showModal: boolean = false;
  confirmationEmail: string = '';
  errorMessage: string = '';
  isDeleting: boolean = false;

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.userEmail = this.authService.getCurrentUserEmail() || '';
  }

  onOpenModal(): void {
    this.showModal = true;
    this.confirmationEmail = '';
    this.errorMessage = '';
  }

  onCloseModal(): void {
    this.showModal = false;
    this.confirmationEmail = '';
    this.errorMessage = '';
  }

  onDeleteAccount(): void {
    if (this.confirmationEmail.trim().toLowerCase() !== this.userEmail.trim().toLowerCase()) {
      this.errorMessage = 'Wpisany adres e-mail nie zgadza się z Twoim adresem e-mail.';
      return;
    }

    this.isDeleting = true;
    this.errorMessage = '';

    this.authService.deleteAccount().subscribe({
      next: () => {
        this.authService.logout();
        this.showModal = false;
        this.isDeleting = false;
        this.router.navigate(['/register'], { queryParams: { deleted: 'true' } });
      },
      error: (err) => {
        this.isDeleting = false;
        this.errorMessage = err.error?.error || 'Wystąpił błąd podczas usuwania konta. Spróbuj ponownie później.';
      }
    });
  }
}
