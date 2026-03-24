import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { EntraRedirectService } from '@app/core/services/entra-redirect.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  private entraService = inject(EntraRedirectService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  async ngOnInit(): Promise<void> {
    // Quand on vient de la landing, on redirige immédiatement vers Entra.
    // PKCE est généré par MSAL (pas d'URL authorize construite à la main).
    const autoEntra = this.route.snapshot.queryParamMap.get('autoEntra');
    if (autoEntra === '1') {
      // Empêche une boucle de relance auto si la page est rafraîchie.
      window.history.replaceState({}, '', '/login');
      await this.entraService.loginWithEntra();
    }
  }

  onMicrosoftLogin(): Promise<void> {
    return this.entraService.loginWithMicrosoft();
  }

  onGoogleLogin(): Promise<void> {
    return this.entraService.loginWithGoogle();
  }
}