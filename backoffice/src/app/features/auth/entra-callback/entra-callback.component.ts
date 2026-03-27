import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';
import { EntraRedirectService } from '../../../services/entra-redirect.service';

@Component({
  selector: 'app-entra-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-4 bg-slate-50">
      <div
        class="h-10 w-10 animate-spin rounded-full border-4 border-slate-200 border-t-blue-600"></div>
      <p class="text-sm font-medium text-slate-600">Authentification en cours...</p>
    </div>
  `,
})
export class EntraCallbackComponent implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  private entraService = inject(EntraRedirectService);

  async ngOnInit(): Promise<void> {
    const result = await this.entraService.handleRedirectPromise();

    console.log('MSAL result:', result);
    console.log('idTokenClaims:', result?.idTokenClaims);
    console.log('idTokenClaims keys:', Object.keys(result?.idTokenClaims ?? {}));

    if (!result) {
      this.router.navigate(['/login']);
      return;
    }

    const account = result.account;
    const email =
      (account.username as string) || (account.idTokenClaims?.['email'] as string);
    const oid = account.idTokenClaims?.['oid'] as string;

    if (!email || !oid) {
      this.router.navigate(['/login'], { queryParams: { error: 'missing_claims' } });
      return;
    }

    this.authService.loginWithEntra(email, oid).subscribe({
      error: () =>
        this.router.navigate(['/login'], { queryParams: { error: 'auth_failed' } }),
    });
  }
}
