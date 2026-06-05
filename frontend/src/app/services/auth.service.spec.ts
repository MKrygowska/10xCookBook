import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('AuthService', () => {
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });
    authService = TestBed.inject(AuthService);
  });

  describe('Token Expiration', () => {
    beforeEach(() => {
      localStorage.clear();
    });

    afterEach(() => {
      localStorage.clear();
    });

    it('should return false for isAuthenticated when token is expired and clear storage', () => {
      const expiredToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjEwMDAwMDAwMDB9.dummy';
      localStorage.setItem('auth_token', expiredToken);
      localStorage.setItem('auth_email', 'test@test.com');

      const result = authService.isAuthenticated();
      expect(result).toBe(false);
      expect(localStorage.getItem('auth_token')).toBeNull();
    });

    it('should return true for isAuthenticated when token is not expired', () => {
      const validToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjk5OTk5OTk5OTl9.dummy';
      localStorage.setItem('auth_token', validToken);
      localStorage.setItem('auth_email', 'test@test.com');

      const result = authService.isAuthenticated();
      expect(result).toBe(true);
    });
  });
});
