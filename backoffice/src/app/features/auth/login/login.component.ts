import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { EntraRedirectService } from '../../../services/entra-redirect.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private entra = inject(EntraRedirectService);

  error = signal('');
  isLoading = signal(false);

  async ngOnInit(): Promise<void> {
    console.log('[AUTH-FLOW][LOGIN] ngOnInit');
    const qp = this.route.snapshot.queryParamMap;
    console.log('[AUTH-FLOW][LOGIN] queryParams', {
      reason: qp.get('reason'),
      error: qp.get('error'),
    });
    const reason = qp.get('reason');
    if (reason === 'expired') {
      this.error.set('Votre session a expiré. Veuillez vous reconnecter.');
    } else if (reason === 'unauthorized') {
      this.error.set('Accès refusé ou session invalide.');
    }
    const err = qp.get('error');
    if (err === 'auth_failed') {
      this.error.set('La connexion a échoué. Votre compte n\'a peut-être pas les droits Admin Payzen. Réessayez.');
    }
    if (err === 'missing_claims') {
      this.error.set('Informations de compte incomplètes. Réessayez.');
    }
    if (err === 'no_result') {
      this.error.set('La réponse Microsoft n\'a pas pu être lue. Vérifiez la configuration Azure (Redirect URI).');
    }
  }

  async signIn(): Promise<void> {
    console.log('[AUTH-FLOW][LOGIN] signIn click');
    this.isLoading.set(true);

    try {
      console.log('[AUTH-FLOW][LOGIN] calling EntraRedirectService.loginWithEntra');
      await this.entra.loginWithEntra();
    } catch {
      console.error('[AUTH-FLOW][LOGIN] loginWithEntra failed');
      this.isLoading.set(false);
      this.error.set('La redirection vers Microsoft a échoué. Réessayez.');
    }
  }
}
