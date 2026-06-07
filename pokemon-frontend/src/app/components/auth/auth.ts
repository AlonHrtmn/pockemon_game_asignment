import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth.html',
  styleUrl: './auth.css'
})
export class AuthComponent {
  isLoginMode = true;
  username = '';
  password = '';
  errorMessage = '';
  successMessage = '';
  isLoading = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    // If already logged in, bypass auth screen
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  toggleMode(): void {
    this.isLoginMode = !this.isLoginMode;
    this.errorMessage = '';
    this.successMessage = '';
    this.username = '';
    this.password = '';
    this.cdr.markForCheck();
  }

  onSubmit(): void {
    if (this.isLoading) {
      return;
    }
    this.errorMessage = '';
    this.successMessage = '';

    const username = this.username.trim();
    const password = this.password.trim();

    if (!username && !password) {
      this.errorMessage = 'Please enter both username and password.';
      this.cdr.markForCheck();
      return;
    }
    if (!username) {
      this.errorMessage = 'Please enter a username.';
      this.cdr.markForCheck();
      return;
    }
    if (!password) {
      this.errorMessage = 'Please enter a password.';
      this.cdr.markForCheck();
      return;
    }
    if (username.length < 3) {
      this.errorMessage = 'Username must be at least 3 characters.';
      this.cdr.markForCheck();
      return;
    }
    if (password.length < 4) {
      this.errorMessage = 'Password must be at least 4 characters.';
      this.cdr.markForCheck();
      return;
    }

    this.isLoading = true;
    this.cdr.markForCheck();

    const credentials = { username: this.username, password: this.password };

    if (this.isLoginMode) {
      this.authService.login(credentials).subscribe({
        next: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          this.isLoading = false;
          const msg = err.error?.message;
          if (err.status === 401) {
            if (msg === 'User not found') {
              this.errorMessage = 'User not found. Please check your username or register a new account.';
            } else if (msg === 'Wrong password') {
              this.errorMessage = 'Wrong password. Please try again.';
            } else {
              this.errorMessage = 'Login failed. Please verify your credentials.';
            }
          } else if (err.status === 503 || err.status === 0) {
            this.errorMessage = 'Service temporarily unavailable. Please try again later.';
          } else {
            this.errorMessage = msg || 'Login failed. Please verify your credentials.';
          }
          this.cdr.markForCheck();
        }
      });
    } else {
      this.authService.register(credentials).subscribe({
        next: (res) => {
          this.successMessage = 'Registration successful! Accessing portal...';
          this.cdr.markForCheck();
          // Automatically log in on success
          this.authService.login(credentials).subscribe({
            next: () => {
              this.isLoading = false;
              this.cdr.markForCheck();
              this.router.navigate(['/dashboard']);
            },
            error: (err) => {
              this.isLoading = false;
              this.isLoginMode = true;
              this.errorMessage = 'Account created, please enter access code manually.';
              this.cdr.markForCheck();
            }
          });
        },
        error: (err) => {
          this.isLoading = false;
          const msg = err.error?.message;
          if (err.status === 409) {
            this.errorMessage = 'Username already exists. Please choose a different username.';
          } else if (err.status === 503 || err.status === 0) {
            this.errorMessage = 'Service temporarily unavailable. Please try again later.';
          } else {
            this.errorMessage = msg || 'Registration failed. Please try again.';
          }
          this.cdr.markForCheck();
        }
      });
    }
  }
}
