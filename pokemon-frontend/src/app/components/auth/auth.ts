import { Component } from '@angular/core';
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

  constructor(private authService: AuthService, private router: Router) {
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
  }

  onSubmit(): void {
    if (!this.username.trim() || !this.password.trim()) {
      this.errorMessage = 'Please enter both username and password.';
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.isLoading = true;

    const credentials = { username: this.username, password: this.password };

    if (this.isLoginMode) {
      this.authService.login(credentials).subscribe({
        next: () => {
          this.isLoading = false;
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = err.error?.message || 'Login failed. Please verify your credentials.';
        }
      });
    } else {
      this.authService.register(credentials).subscribe({
        next: (res) => {
          this.successMessage = 'Registration successful! Accessing portal...';
          // Automatically log in on success
          this.authService.login(credentials).subscribe({
            next: () => {
              this.isLoading = false;
              this.router.navigate(['/dashboard']);
            },
            error: (err) => {
              this.isLoading = false;
              this.isLoginMode = true;
              this.errorMessage = 'Account created, please enter access code manually.';
            }
          });
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = err.error?.message || 'Registration failed. Try a different username.';
        }
      });
    }
  }
}
