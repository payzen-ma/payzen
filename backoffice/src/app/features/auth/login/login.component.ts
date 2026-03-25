import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnInit {
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  email = '';
  password = '';
  error = signal('');
  isLoading = signal(false);

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const reason = qp.get('reason');
    if (reason === 'expired') {
      this.error.set('Votre session a expiré. Veuillez vous reconnecter.');
    } else if (reason === 'unauthorized') {
      this.error.set('Accès refusé ou session invalide.');
    }
    const err = qp.get('error');
    if (err === 'auth_failed') {
      this.error.set('La connexion a échoué. Réessayez.');
    }
    if (err === 'missing_claims') {
      this.error.set('Informations de compte incomplètes. Réessayez.');
    }
  }

  onSubmit(): void {
    if (this.isLoading()) return;
    const email = this.email.trim();
    const password = this.password;
    if (!email || !password) return;

    this.error.set('');
    this.isLoading.set(true);

    this.auth.login(email, password).subscribe({
      next: () => {
        this.isLoading.set(false);
        void this.router.navigate(['/dashboard']);
      },
      error: (err: unknown) => {
        this.isLoading.set(false);
        const e = err as { message?: string; error?: { Message?: string; message?: string } };
        const msg =
          e?.message ??
          e?.error?.Message ??
          e?.error?.message ??
          'Email ou mot de passe incorrect.';
        this.error.set(msg);
      },
    });
  }
}
