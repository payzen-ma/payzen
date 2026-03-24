import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { Router } from '@angular/router';
import { AuthService } from '@app/core/services/auth.service';
import { EntraOtpService } from '@app/core/services/entra-otp.service';

@Component({
  selector: 'app-entra-callback',
  standalone: true,
  imports: [CommonModule, TranslateModule, ButtonModule],
  template: `
    <div class="min-h-screen flex items-center justify-center">
      <div class="card p-6 text-center">
        <h2 class="text-xl font-semibold mb-2">{{ 'auth.login.entraCallback.title' | translate }}</h2>
        <p class="text-sm text-gray-600 mb-4">{{ status() }}</p>

        @if (error()) {
          <div class="text-sm text-red-600 mb-4">
            {{ error() }}
          </div>
          <button pButton class="btn btn-primary" [label]="'common.goBack' | translate" (click)="goBack()"></button>
        }
      </div>
    </div>
  `
})
export class EntraCallbackComponent implements OnInit {
  readonly status = signal<string>('Connexion Entra en cours...');
  readonly error = signal<string | null>(null);

  constructor(
    private authService: AuthService,
    private entraOtpService: EntraOtpService,
    private router: Router
  ) {}

  async ngOnInit(): Promise<void> {
    try {
      console.debug('[EntraCallback] ngOnInit URL:', window.location.href);
      const result = await this.entraOtpService.handleRedirect();
      console.debug('[EntraCallback] result:', result);

      if (!result) {
        const query = window.location.search || '(vide)';
        const hash = window.location.hash || '(vide)';
        this.status.set('Aucune réponse MSAL reçue.');
        this.error.set(`Debug callback: result=null | query=${query} | hash=${hash}`);
        return;
      }

      const claims: any = result.idTokenClaims ?? {};

      const externalId =
        claims?.oid ??
        claims?.['http://schemas.microsoft.com/identity/claims/objectidentifier'] ??
        claims?.objectidentifier ??
        null;

      const email =
        claims?.email ??
        claims?.preferred_username ??
        claims?.upn ??
        claims?.unique_name ??
        null;

      console.debug('[EntraCallback] claims:', claims);
      console.debug('[EntraCallback] email:', email, '| externalId:', externalId);

      if (!externalId || !email) {
        throw new Error(`Claims Entra insuffisants — email: ${email}, externalId: ${externalId}`);
      }

      this.status.set('Finalisation de la connexion...');

      this.authService.loginWithEntra(String(email), String(externalId)).subscribe({
        next: () => { /* navigation gérée par AuthService.handleLoginSuccess */ },
        error: (err) => {
          const msg =
            err?.error?.Message ?? err?.error?.message ??
            err?.error?.Error ?? err?.error?.error ??
            err?.Message ?? err?.message ?? 'Echec de la connexion.';
          console.error('[EntraCallback] erreur backend:', err);
          this.error.set(String(msg));
          this.status.set('');
        }
      });
    } catch (e: any) {
      console.error('[EntraCallback] exception:', e);
      this.error.set(e?.message ? String(e.message) : String(e));
      this.status.set('');
    }
  }

  goBack() {
    this.router.navigate(['/login']);
  }
}

