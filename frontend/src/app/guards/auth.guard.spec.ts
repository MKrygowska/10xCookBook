import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { routes } from '../app.routes';

describe('authGuard', () => {
  let authService: AuthService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } }
      ]
    });
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('should allow activation when user is authenticated', () => {
    spyOn(authService, 'isAuthenticated').and.returnValue(true);
    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));
    expect(result).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should block activation and redirect to login when user is not authenticated', () => {
    spyOn(authService, 'isAuthenticated').and.returnValue(false);
    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));
    expect(result).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  describe('Route Guard Configuration', () => {
    it('should protect /dashboard, /my-recipes, and /settings routes with authGuard', () => {
      const protectedPaths = ['dashboard', 'my-recipes', 'settings'];
      
      protectedPaths.forEach(path => {
        const route = routes.find(r => r.path === path);
        expect(route).toBeDefined();
        expect(route?.canActivate).toContain(authGuard);
      });
    });
  });
});
