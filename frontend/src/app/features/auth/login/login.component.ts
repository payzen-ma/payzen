import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EntraRedirectService } from '@app/core/services/entra-redirect.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (routeError(); as err) {
      <div class="redirect-container error-panel">
        @if (err === 'idp_user_not_in_tenant') {
          <h1>Compte non reconnu dans Payzen HR</h1>
          <p>
            Votre compte Google n’est pas encore enregistré dans l’annuaire Payzen HR (Microsoft Entra External ID).
            Il faut une <strong>première inscription</strong> dans ce tenant, ou une configuration côté administrateur Azure.
          </p>
          <p class="hint">
            Côté portail : fournisseur d’identité <strong>Google</strong>, user flow <strong>Sign up and sign in</strong> avec
            inscription activée, et application SPA liée au flux.
          </p>
        } @else {
          <h1>Connexion impossible</h1>
          <p>Une erreur s’est produite lors de l’authentification. Vous pouvez réessayer.</p>
        }
        <div class="actions">
          <button type="button" class="btn primary" (click)="goSignup()">Inscription (Google / Microsoft)</button>
          <button type="button" class="btn secondary" (click)="goRetryLogin()">Connexion</button>
        </div>
      </div>
    } @else {
      <div class="redirect-container">
        <div class="spinner"></div>
        <p>Redirection vers la connexion...</p>
      </div>
    }
  `,
  styles: [
    `
    .redirect-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      gap: 1rem;
      padding: 1.5rem;
      max-width: 32rem;
      margin: 0 auto;
      background: #f8f9fc;
      color: #3a3c50;
      font-family: 'DM Sans', sans-serif;
    }
    .error-panel h1 {
      font-size: 1.25rem;
      font-weight: 600;
      text-align: center;
      margin: 0;
    }
    .error-panel p {
      font-size: 0.95rem;
      line-height: 1.5;
      margin: 0;
      text-align: center;
    }
    .hint {
      font-size: 0.85rem !important;
      color: #5c5f72;
    }
    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
      justify-content: center;
      margin-top: 0.5rem;
    }
    .btn {
      border: none;
      border-radius: 8px;
      padding: 0.6rem 1.1rem;
      font-size: 0.9rem;
      font-weight: 600;
      cursor: pointer;
      font-family: inherit;
    }
    .btn.primary {
      background: #2a2ce0;
      color: #fff;
    }
    .btn.secondary {
      background: #e4e6ef;
      color: #3a3c50;
    }
    .spinner {
      width: 40px;
      height: 40px;
      border: 4px solid #e4e6ef;
      border-top: 4px solid #2a2ce0;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
  `,
  ],
})
export class LoginComponent implements OnInit {
  private entraService = inject(EntraRedirectService);
  private route = inject(ActivatedRoute);

  /** Affiché si ?error=… (pas de redirect Entra pour éviter une boucle). */
  routeError = signal<'idp_user_not_in_tenant' | 'auth_failed' | null>(null);

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const error = qp.get('error');
    if (error === 'idp_user_not_in_tenant') {
      this.routeError.set('idp_user_not_in_tenant');
      return;
    }
    if (error === 'auth_failed' || error === 'invite_failed' || error === 'missing_claims') {
      this.routeError.set('auth_failed');
      return;
    }

    void this.startEntra(qp.get('mode'));
  }

  private async startEntra(mode: string | null): Promise<void> {
    try {
      if (mode === 'signup') {
        await this.entraService.signupWithEntra();
      } else {
        await this.entraService.loginWithEntra();
      }
    } catch {
      this.routeError.set('auth_failed');
    }
  }

  goSignup(): void {
    this.routeError.set(null);
    void this.startEntra('signup');
  }

  goRetryLogin(): void {
    this.routeError.set(null);
    void this.startEntra(null);
  }
}
