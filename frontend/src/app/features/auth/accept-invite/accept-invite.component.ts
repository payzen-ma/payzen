import { Component, OnInit, signal, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { InvitationService, InvitationInfo } from '@app/core/services/invitation.service';
import { EntraRedirectService } from '@app/core/services/entra-redirect.service';

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './accept-invite.component.html',
  styleUrls: ['./accept-invite.component.css'],
})
export class AcceptInviteComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private invitationService = inject(InvitationService);
  private entraService = inject(EntraRedirectService);

  /** Étape affichée : validation API puis redirection IdP (sans clic). */
  phase = signal<'validating' | 'redirecting' | 'error'>('validating');
  error = signal<string | null>(null);
  private token = '';

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    const errorParam = this.route.snapshot.queryParamMap.get('error');
    const message = this.route.snapshot.queryParamMap.get('message');

    if (errorParam === 'email_mismatch') {
      this.phase.set('error');
      this.error.set(message ?? "L'email de votre compte ne correspond pas à l'invitation.");
      return;
    }

    if (!this.token) {
      this.phase.set('error');
      this.error.set("Token d'invitation manquant.");
      return;
    }

    this.invitationService.validate(this.token).subscribe({
      next: (info) => {
        void this.redirectToIdpAfterValidation(info);
      },
      error: () => {
        this.phase.set('error');
        this.error.set('Invitation invalide ou expirée.');
      },
    });
  }

  /**
   * Pas d'écran intermédiaire : après validation, on enchaîne tout de suite vers CIAM.
   * Gmail → Google IdP ; sinon parcours Entra standard (Microsoft / e-mail OTP selon config tenant).
   */
  private async redirectToIdpAfterValidation(info: InvitationInfo): Promise<void> {
    this.phase.set('redirecting');
    sessionStorage.setItem('pending_invitation_token', this.token);

    const masked = (info.maskedEmail || '').toLowerCase();
    const useGoogle =
      masked.includes('@gmail.') || masked.includes('googlemail');

    try {
      if (useGoogle) {
        await this.entraService.loginForInviteAcceptanceWithGoogle();
      } else {
        await this.entraService.signupWithEntra();
      }
    } catch {
      this.phase.set('error');
      this.error.set(
        'Impossible de lancer la connexion. Réessayez en rouvrant le lien d’invitation.'
      );
      sessionStorage.removeItem('pending_invitation_token');
    }
  }
}
