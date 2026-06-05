import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = window.location.hostname === 'localhost' 
    ? 'http://localhost:5174/api' 
    : 'https://cookbook-api-unique.azurewebsites.net/api';

  private readonly tokenKey = 'auth_token';
  private readonly emailKey = 'auth_email';
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(private http: HttpClient) {}

  register(credentials: { email: string; password: string }): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/auth/register`, credentials).pipe(
      tap(res => this.handleAuthSuccess(res))
    );
  }

  login(credentials: { email: string; password: string }): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/auth/login`, credentials).pipe(
      tap(res => this.handleAuthSuccess(res))
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.emailKey);
    this.isAuthenticatedSubject.next(false);
  }

  deleteAccount(): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/users/me`);
  }

  isAuthenticated(): boolean {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) {
      this.isAuthenticatedSubject.next(false);
      return false;
    }
    const isExpired = this.isTokenExpired(token);
    if (isExpired) {
      this.logout();
      return false;
    }
    return this.isAuthenticatedSubject.value;
  }

  getCurrentUserEmail(): string | null {
    return localStorage.getItem(this.emailKey);
  }

  private isTokenExpired(token: string): boolean {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) {
        return true;
      }
      const payload = JSON.parse(atob(parts[1]));
      if (payload && typeof payload.exp === 'number') {
        const expiryDate = payload.exp * 1000;
        return expiryDate < Date.now();
      }
      return false;
    } catch {
      return true;
    }
  }

  private hasToken(): boolean {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) {
      return false;
    }
    return !this.isTokenExpired(token);
  }

  private handleAuthSuccess(res: any): void {
    if (res && res.token) {
      localStorage.setItem(this.tokenKey, res.token);
      localStorage.setItem(this.emailKey, res.email);
      this.isAuthenticatedSubject.next(true);
    }
  }
}
