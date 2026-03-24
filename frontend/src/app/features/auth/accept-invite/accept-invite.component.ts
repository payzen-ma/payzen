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
  styleUrls: ['./accept-invite.component.css']
})
export class AcceptInviteComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private invitationService = inject(InvitationService);
  private entraService = inject(EntraRedirectService);

  invitation = signal<InvitationInfo | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  token = '';

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    const errorParam = this.route.snapshot.queryParamMap.get('error');
    const message = this.route.snapshot.queryParamMap.get('message');

    if (errorParam === 'email_mismatch') {
      this.error.set(message ?? "L'email de votre compte ne correspond pas à l'invitation.");
    }

    if (!this.token) {
      this.error.set('Token d\'invitation manquant.');
      this.isLoading.set(false);
      return;
    }

    this.invitationService.validate(this.token).subscribe({
      next: (info) => {
        this.invitation.set(info);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Invitation invalide ou expirée.');
        this.isLoading.set(false);
      }
    });
  }

  acceptWithMicrosoft(): void {
    sessionStorage.setItem('pending_invitation_token', this.token);
    this.entraService.loginWithMicrosoft();
  }

  acceptWithGoogle(): void {
    sessionStorage.setItem('pending_invitation_token', this.token);
    this.entraService.loginWithGoogle();
  }
}