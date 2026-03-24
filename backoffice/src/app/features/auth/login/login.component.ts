import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html'
})
export class LoginComponent implements OnInit {
  email = '';
  password = '';
  isLoading = signal(false);
  error = signal('');

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const reason = this.route.snapshot.queryParamMap.get('reason');
    if (reason === 'expired') {
      this.error.set('Votre session a expire. Veuillez vous reconnecter.');
    } else if (reason === 'unauthorized') {
      this.error.set('Acces refuse. Veuillez vous reconnecter.');
    }
  }

  onSubmit(): void {
    this.login();
  }

  login(): void {
    if (!this.email || !this.password) {
      this.error.set('Veuillez saisir votre email et mot de passe');
      return;
    }

    this.isLoading.set(true);
    this.error.set('');

    this.authService.login(this.email, this.password).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading.set(false);
        
        if (err.message && err.message.includes('admin payzen')) {
          this.error.set('Accès refusé. Seuls les administrateurs PayZen peuvent se connecter.');
        } else if (err.status === 401) {
          this.error.set('Email ou mot de passe incorrect');
        } else if (err.status === 0) {
          this.error.set('Impossible de se connecter au serveur. Vérifiez que l\'API est démarrée.');
        } else {
          this.error.set('Une erreur est survenue lors de la connexion');
        }
        
        console.error('Login error:', err);
      }
    });
  }
}
