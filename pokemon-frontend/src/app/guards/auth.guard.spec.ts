import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let authServiceMock: any;
  let routerMock: any;

  beforeEach(() => {
    authServiceMock = {
      isLoggedIn: () => false
    };

    routerMock = {
      navigate: () => {}
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });
  });

  it('should return true if the user is logged in', () => {
    authServiceMock.isLoggedIn = () => true;
    
    // Run the guard in the TestBed injection context
    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));
    
    expect(result).toBe(true);
  });

  it('should navigate to /auth and return false if the user is not logged in', () => {
    authServiceMock.isLoggedIn = () => false;
    const navigateSpy = vi.spyOn(routerMock, 'navigate');

    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));

    expect(result).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/auth']);
  });
});
