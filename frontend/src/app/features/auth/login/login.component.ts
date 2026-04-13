import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EntraRedirectService } from '@app/core/services/entra-redirect.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (routeError(); as err) {
      <div class="login-error-wrapper">
        <div class="bg-decoration"></div>
        <div class="login-error-card">
          <div class="error-header">
            <div class="error-icon-container">
              <svg class="error-icon" viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
                <circle cx="50" cy="50" r="45" fill="none" stroke="#2a2ce0" stroke-width="2" opacity="0.1"/>
                <circle cx="50" cy="50" r="35" fill="none" stroke="#2a2ce0" stroke-width="1.5" opacity="0.2"/>
                <text x="50" y="65" font-size="45" text-anchor="middle" fill="#2a2ce0">⚡</text>
              </svg>
            </div>
            <h1>Oups!</h1>
          </div>
          <div class="error-content">
            @if (err === 'idp_user_not_in_tenant') {
              <p class="error-title">Compte non reconnu</p>
              <p class="error-description">
                Votre compte n'existe pas encore dans Payzen HR.
                Créez-en un rapidement, c'est gratuit !
              </p>
            } @else {
              <p class="error-title">Erreur d'authentification</p>
              <p class="error-description">
                La connexion a échoué.
                <br><strong>Réessayez ou créez un nouveau compte.</strong>
              </p>
            }
          </div>
          <div class="error-actions">
            <button type="button" class="btn btn-primary" (click)="goRetryLogin()">
              <span class="btn-icon">↻</span>
              <span class="btn-text">Réessayer la connexion</span>
            </button>
            <button type="button" class="btn btn-secondary" (click)="goSignup()">
              <span class="btn-icon">+</span>
              <span class="btn-text">Créer un compte</span>
            </button>
          </div>
          <div class="error-footer">
            <p class="footer-text">
              Une question ? <a href="mailto:support@payzenhr.com" class="support-link">Contactez-nous</a>
            </p>
          </div>
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
    /* === PAGE D'ERREUR === */
    .login-error-wrapper {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      padding: 1.5rem;
      background: linear-gradient(135deg, #f8f9fc 0%, #f0f4ff 100%);
      position: relative;
      overflow: hidden;
      font-family: 'DM Sans', sans-serif;
    }

    .bg-decoration {
      position: absolute;
      top: -100px;
      right: -100px;
      width: 300px;
      height: 300px;
      background: radial-gradient(circle, rgba(42, 44, 224, 0.08) 0%, transparent 70%);
      border-radius: 50%;
      pointer-events: none;
    }

    .login-error-card {
      background: white;
      border-radius: 16px;
      padding: 2.5rem 2rem;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.08);
      max-width: 420px;
      width: 100%;
      position: relative;
      z-index: 10;
      animation: slideUp 0.4s ease-out;
    }

    @keyframes slideUp {
      from {
        opacity: 0;
        transform: translateY(30px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .error-header {
      text-align: center;
      margin-bottom: 1.5rem;
    }

    .error-icon-container {
      display: flex;
      justify-content: center;
      margin-bottom: 1rem;
    }

    .error-icon {
      width: 80px;
      height: 80px;
      animation: bounce 2s ease-in-out infinite;
    }

    @keyframes bounce {
      0%, 100% { transform: translateY(0); }
      50% { transform: translateY(-10px); }
    }

    .error-header h1 {
      font-size: 2rem;
      font-weight: 700;
      margin: 0;
      color: #2a2ce0;
      letter-spacing: -0.5px;
    }

    .error-content {
      margin-bottom: 2rem;
      text-align: center;
    }

    .error-title {
      font-size: 1.25rem;
      font-weight: 600;
      color: #2a2ce0;
      margin: 0 0 0.75rem 0;
    }

    .error-description {
      font-size: 0.9rem;
      line-height: 1.6;
      color: #5c5f72;
      margin: 0;
    }

    .error-description strong {
      color: #2a2ce0;
      font-weight: 600;
    }

    /* === BOUTONS === */
    .error-actions {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      margin-bottom: 1.5rem;
    }

    .btn {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      border: none;
      border-radius: 10px;
      padding: 0.85rem 1.25rem;
      font-size: 0.95rem;
      font-weight: 600;
      cursor: pointer;
      font-family: inherit;
      transition: all 0.3s ease;
      position: relative;
      overflow: hidden;
    }

    .btn::before {
      content: '';
      position: absolute;
      top: 50%;
      left: 50%;
      width: 0;
      height: 0;
      background: rgba(255, 255, 255, 0.3);
      border-radius: 50%;
      transform: translate(-50%, -50%);
      transition: width 0.6s, height 0.6s;
    }

    .btn:hover::before {
      width: 300px;
      height: 300px;
    }

    .btn-primary {
      background: linear-gradient(135deg, #2a2ce0 0%, #1a1cc0 100%);
      color: white;
      box-shadow: 0 4px 15px rgba(42, 44, 224, 0.3);
    }

    .btn-primary:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 20px rgba(42, 44, 224, 0.4);
    }

    .btn-secondary {
      background: #f0f2ff;
      color: #2a2ce0;
      border: 2px solid #e4e6ef;
    }

    .btn-secondary:hover {
      background: #e8ebff;
      border-color: #2a2ce0;
      transform: translateY(-2px);
    }

    .btn-icon {
      font-size: 1.1rem;
      font-weight: bold;
      position: relative;
      z-index: 1;
    }

    .btn-text {
      font-weight: 600;
      position: relative;
      z-index: 1;
    }

    /* === FOOTER === */
    .error-footer {
      border-top: 1px solid #e4e6ef;
      padding-top: 1rem;
      text-align: center;
    }

    .footer-text {
      font-size: 0.85rem;
      color: #8a8d9f;
      margin: 0;
    }

    .support-link {
      color: #2a2ce0;
      text-decoration: none;
      font-weight: 600;
      transition: color 0.2s ease;
    }

    .support-link:hover {
      color: #1a1cc0;
      text-decoration: underline;
    }

    /* === REDIRECT LOADING STATE === */
    .redirect-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      gap: 1rem;
      padding: 1.5rem;
      background: linear-gradient(135deg, #f8f9fc 0%, #f0f4ff 100%);
      color: #3a3c50;
      font-family: 'DM Sans', sans-serif;
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

    /* === RESPONSIVE === */
    @media (max-width: 480px) {
      .login-error-card {
        padding: 1.5rem;
      }

      .error-header h1 {
        font-size: 1.5rem;
      }

      .error-icon {
        width: 60px;
        height: 60px;
      }

      .btn {
        padding: 0.75rem 1rem;
        font-size: 0.9rem;
      }
    }
  `,
  ],
})
export class LoginComponent implements OnInit {
  private entraService = inject(EntraRedirectService);
  private route = inject(ActivatedRoute);

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
