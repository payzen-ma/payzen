import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '@environments/environment';
import { AuthService } from '@app/core/services/auth.service';

@Component({
  selector: 'app-company-signup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './company-signup.component.html',
  styleUrls: ['./company-signup.component.css'],
})
export class CompanySignupComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  private readonly apiUrl = environment.apiUrl;

  readonly isLoadingCountries = signal(false);
  readonly isSubmitting = signal(false);

  readonly error = signal<string | null>(null);

  // Formulaire public minimal demandé
  readonly form = {
    CompanyName: '',
    AdminFirstName: '',
    AdminLastName: '',
    AdminPhone: '',
  };

  ngOnInit(): void {
    const currentUser = this.auth.getCurrentUser();
    if (!currentUser?.email) {
      this.router.navigate(['/login'], { queryParams: { mode: 'signup' } });
      return;
    }
  }

  private validateClientSide(): string | null {
    const f = this.form;
    if (!f.CompanyName.trim()) return 'Le nom de l’entreprise est requis.';

    if (!f.AdminFirstName.trim()) return "Le prénom de l'administrateur est requis.";
    if (!f.AdminLastName.trim()) return "Le nom de l'administrateur est requis.";
    if (!f.AdminPhone.trim()) return "Le téléphone de l’administrateur est requis.";

    return null;
  }

  submit(): void {
    this.error.set(null);

    const clientError = this.validateClientSide();
    if (clientError) {
      this.error.set(clientError);
      return;
    }

    this.isSubmitting.set(true);

    const payload: any = {
      CompanyName: this.form.CompanyName,
      AdminFirstName: this.form.AdminFirstName,
      AdminLastName: this.form.AdminLastName,
      AdminPhone: this.form.AdminPhone
    };

    this.http.post<any>(`${this.apiUrl}/public/complete-company-onboarding`, payload).subscribe({
      next: (r) => {
        const user = this.auth.getCurrentUser();
        const oid = localStorage.getItem('payzen_entra_oid');
        const email = user?.email ?? null;

        if (!email || !oid) {
          // Fallback: navigate anyway (guards may redirect if needed)
          this.isSubmitting.set(false);
          this.router.navigate(['/app/dashboard']);
          return;
        }

        // Refresh JWT so it includes companyId/roles after onboarding.
        this.auth.loginWithEntra(email, oid).subscribe({
          next: () => {
            this.isSubmitting.set(false);
          },
          error: () => {
            this.isSubmitting.set(false);
            // Even if refresh fails, try to proceed.
            this.router.navigate(['/app/dashboard']);
          }
        });
      },
      error: (err: any) => {
        this.isSubmitting.set(false);
        this.error.set(err?.error?.Message ?? 'Impossible de créer l’entreprise.');
      },
    });
  }

  goLogin(): void {
    this.router.navigate(['/login']);
  }
}

