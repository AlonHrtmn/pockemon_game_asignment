import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

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
  private apiUrl = 'http://localhost:5072/api/auth';
  
  // Use modern Angular Signals to track the logged-in user state reactively
  currentUser = signal<string | null>(localStorage.getItem('trainer_username'));

  constructor(private http: HttpClient) {}

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
    localStorage.removeItem('trainer_token');
    localStorage.removeItem('trainer_username');
    this.currentUser.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem('trainer_token');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }
}
