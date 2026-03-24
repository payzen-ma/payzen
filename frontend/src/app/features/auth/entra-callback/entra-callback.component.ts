import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@app/core/services/auth.service';
import { EntraRedirectService } from '@app/core/services/entra-redirect.service';
import { InvitationService } from '@app/core/services/invitation.service';
import { switchMap } from 'rxjs/operators';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-entra-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="callback-container">
      <div class="spinner"></div>
      <p>Authentification en cours...</p>
    </div>
  `,
  styles: [`
    .callback-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      gap: 1rem;
    }
    .spinner {
      width: 40px;
      height: 40px;
      border: 4px solid #f3f3f3;
      border-top: 4px solid #3498db;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
  `]
})
export class EntraCallbackComponent implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  private entraService = inject(EntraRedirectService);
  private invitationService = inject(InvitationService);

  async ngOnInit() {
    console.log('🔄 EntraCallback: handleRedirectPromise...');
    
    const result = await this.entraService.handleRedirectPromise();
    
    if (!result) {
      console.log('❌ No result from redirect');
      this.router.navigate(['/login']);
      return;
    }
    console.log('✅ Redirect result:', result);
    console.log('📧 Email:', result.account.username);
    console.log('🆔 OID:', result.account.idTokenClaims?.['oid']);

    const account = result.account;
    const email = account.username || account.idTokenClaims?.['email'] as string;
    const oid = account.idTokenClaims?.['oid'] as string;

    if (!email || !oid) {
      this.router.navigate(['/login'], { 
        queryParams: { error: 'missing_claims' } 
      });
      return;
    }

    // Cas invitation : token présent dans sessionStorage
    const invitationToken = sessionStorage.getItem('pending_invitation_token');
    
    if (invitationToken) {
      sessionStorage.removeItem('pending_invitation_token');
      this.handleInvitationFlow(invitationToken, email, oid);
      return;
    }

    // Cas connexion normale
    this.handleNormalLogin(email, oid);
  }

  private handleNormalLogin(email: string, oid: string): void {
    this.authService.loginWithEntra(email, oid).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        console.error('Login error:', err);
        this.router.navigate(['/login'], { 
          queryParams: { error: 'auth_failed' } 
        });
      }
    });
  }

  private handleInvitationFlow(token: string, email: string, oid: string): void {
    // D'abord se connecter pour avoir un JWT Payzen
    this.authService.loginWithEntra(email, oid).pipe(
      switchMap(() => this.invitationService.acceptViaIdp(token))
    ).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        if (err?.error?.Code === 'EMAIL_MISMATCH') {
          this.router.navigate(['/auth/accept-invite'], {
            queryParams: { 
              token, 
              error: 'email_mismatch', 
              message: err.error.Message 
            }
          });
        } else {
          this.router.navigate(['/login'], { 
            queryParams: { error: 'invite_failed' } 
          });
        }
      }
    });
  }
}