import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NativeAuthService } from '@app/core/services/native-auth.service';
import { AuthService } from '@app/core/services/auth.service';
import { EntraOtpService } from '@app/core/services/entra-otp.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class LoginPage {
  private nativeAuth  = inject(NativeAuthService);
  private authService = inject(AuthService);
  private entraOtp    = inject(EntraOtpService);
  private router      = inject(Router);

  email       = signal('');
  password    = signal('');
  isLoading   = signal(false);
  error       = signal<string | null>(null);
  showPassword = signal(false);

  // ── Email + mot de passe (Native Auth) ────────────────────────────────────

  onSignIn() {
    if (!this.email() || !this.password() || this.isLoading()) return;
    this.error.set(null);
    this.isLoading.set(true);

    this.nativeAuth.signIn(this.email(), this.password()).subscribe({
      next: (res) => {
        this.authService['handleLoginSuccess'](res);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.error.set(
          err?.error?.Message ?? 'Email ou mot de passe incorrect.'
        );
      }
    });
  }

  // ── Google via Entra (redirect — inévitable avec IdP fédéré) ──────────────

  onGoogleSignIn() {
    this.error.set(null);
    this.entraOtp.redirectToSignIn('google');  // EntraOtpService existant, inchangé
  }

  // ── Navigation ────────────────────────────────────────────────────────────

  goToSignUp() {
    this.router.navigate(['/signup']);
  }

  togglePassword() {
    this.showPassword.update(v => !v);
  }
}