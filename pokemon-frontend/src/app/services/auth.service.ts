import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Credentials {
  username: string;
  password?: string;
}

export interface AuthResponse {
  token: string;
  username: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  
  // Use modern Angular Signals to track the logged-in user state reactively
  currentUser = signal<string | null>(localStorage.getItem('trainer_username'));

  constructor(private http: HttpClient, private router: Router) {}

  register(credentials: Credentials): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, credentials);
  }

  login(credentials: Credentials): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(res => {
        localStorage.setItem('trainer_token', res.token);
        localStorage.setItem('trainer_username', res.username);
        this.currentUser.set(res.username);
      })
    );
  }

  logout(): void {
    const username = this.currentUser();
    localStorage.removeItem('trainer_token');
    localStorage.removeItem('trainer_username');
    if (username) {
      localStorage.removeItem(`team_cache_${username}`);
    }
    this.currentUser.set(null);
    this.router.navigate(['/auth']);
  }

  getToken(): string | null {
    return localStorage.getItem('trainer_token');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }
}
