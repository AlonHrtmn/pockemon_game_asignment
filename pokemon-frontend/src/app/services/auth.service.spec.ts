import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'auth', component: class {} }
        ])
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should store token and username on login', () => {
    const mockResponse = { token: 'mock-jwt-token', username: 'ash_ketchum' };
    
    service.login({ username: 'ash_ketchum', password: 'password' }).subscribe(res => {
      expect(res.token).toBe('mock-jwt-token');
      expect(res.username).toBe('ash_ketchum');
      expect(localStorage.getItem('trainer_token')).toBe('mock-jwt-token');
      expect(localStorage.getItem('trainer_username')).toBe('ash_ketchum');
      expect(service.currentUser()).toBe('ash_ketchum');
    });

    const req = httpMock.expectOne('http://localhost:5088/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should clear token and username on logout', () => {
    localStorage.setItem('trainer_token', 'token');
    localStorage.setItem('trainer_username', 'ash');
    service.currentUser.set('ash');

    service.logout();

    expect(localStorage.getItem('trainer_token')).toBeNull();
    expect(localStorage.getItem('trainer_username')).toBeNull();
    expect(service.currentUser()).toBeNull();
  });
});
