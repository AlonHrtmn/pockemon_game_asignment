import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const token = localStorage.getItem('trainer_token');

    let authReq = req;
    if (token && !req.url.includes('/api/auth/')) {
        authReq = req.clone({
            setHeaders: { Authorization: `Bearer ${token}` }
        });
    }

    return next(authReq).pipe(
        catchError((err) => {
            // Auto-logout on 401 for non-auth endpoints (expired JWT)
            if (err.status === 401 && !req.url.includes('/api/auth/')) {
                authService.logout();
                // Small delay to let navigation complete, then we could show a message
                // The auth component will be shown after redirect
            }
            return throwError(() => err);
        })
    );
};
