import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { NativeAuthService } from '@app/core/services/native-auth.service';
import { environment } from '@environments/environment';

interface InviteInfo {
  email: string;
  companyName: string;
  roleName: string;
  companyId: number;
  roleId: number;
  employeeId?: number;
}

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './signup.html'
})
export class SignupPage implements OnInit {
  private nativeAuth = inject(NativeAuthService);
  private router     = inject(Router);
  private route      = inject(ActivatedRoute);
  private http       = inject(HttpClient);

  // ── State ─────────────────────────────────────────────────────────────────

  email           = signal('');
  password        = signal('');
  confirmPassword = signal('');
  isLoading       = signal(false);
  isLoadingInvite = signal(false);
  error           = signal<string | null>(null);
  success         = signal(false);
  showPassword    = signal(false);
  inviteInfo      = signal<InviteInfo | null>(null);

  // Token d'invitation depuis le query param /signup?token=XXX
  // Exposé en public pour que le template puisse le lire avec !!invitationToken
  invitationToken: string | undefined =
    this.route.snapshot.queryParamMap.get('token') ?? undefined;

  // ── Computed ──────────────────────────────────────────────────────────────

  /**
   * Force du mot de passe : 0 à 4
   * 1 — au moins 8 caractères
   * 2 — + une minuscule et une majuscule
   * 3 — + un chiffre
   * 4 — + un caractère spécial
   */
  passwordStrength = computed<number>(() => {
    const p = this.password();
    if (p.length === 0) return 0;
    let score = 0;
    if (p.length >= 8)                   score++;
    if (/[a-z]/.test(p) && /[A-Z]/.test(p)) score++;
    if (/[0-9]/.test(p))                 score++;
    if (/[^a-zA-Z0-9]/.test(p))         score++;
    return score;
  });

  strengthLabel = computed<string>(() => {
    const labels = ['', 'Faible', 'Moyen', 'Bon', 'Excellent'];
    return labels[this.passwordStrength()] ?? '';
  });

  strengthColor = computed<string>(() => {
    const colors = ['', 'bg-red-400', 'bg-orange-400', 'bg-yellow-400', 'bg-green-500'];
    return colors[this.passwordStrength()] ?? 'bg-gray-200';
  });

  strengthTextColor = computed<string>(() => {
    const colors = ['', 'text-red-500', 'text-orange-500', 'text-yellow-600', 'text-green-600'];
    return colors[this.passwordStrength()] ?? '';
  });

  isFormValid = computed<boolean>(() =>
    this.email().length > 0 &&
    this.password().length >= 8 &&
    this.password() === this.confirmPassword()
  );

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    if (this.invitationToken) {
      this.loadInvitationInfo();
    }
  }

  // ── Invitation ────────────────────────────────────────────────────────────

  /**
   * Valide le token d'invitation et pré-remplit l'email.
   * L'email sera non-éditable dans le template (disabled si invitationToken présent).
   */
  private loadInvitationInfo(): void {
    this.isLoadingInvite.set(true);
    this.http
      .get<InviteInfo>(`${environment.apiUrl}/invitations/validate`, {
        params: { token: this.invitationToken! }
      })
      .subscribe({
        next: (info) => {
          this.inviteInfo.set(info);
          this.email.set(info.email);
          this.isLoadingInvite.set(false);
        },
        error: (err) => {
          this.isLoadingInvite.set(false);
          // Token invalide ou expiré → afficher l'erreur et laisser l'email éditable
          this.invitationToken = undefined;
          this.error.set(
            err?.error?.Message ??
            'Ce lien d\'invitation est invalide ou expiré. Vous pouvez tout de même créer un compte.'
          );
        }
      });
  }

  // ── Signup ────────────────────────────────────────────────────────────────

  onSignUp(): void {
    if (!this.isFormValid() || this.isLoading()) return;

    if (this.password() !== this.confirmPassword()) {
      this.error.set('Les mots de passe ne correspondent pas.');
      return;
    }

    this.error.set(null);
    this.isLoading.set(true);

    this.nativeAuth
      .signUp(this.email(), this.password(), this.invitationToken)
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.success.set(true);
        },
        error: (err) => {
          this.isLoading.set(false);
          this.error.set(
            err?.error?.Message ?? 'Impossible de créer le compte. Réessayez.'
          );
        }
      });
  }

  // ── UI helpers ────────────────────────────────────────────────────────────

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}