import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '@environments/environment';

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

  private readonly apiUrl = environment.apiUrl;

  readonly isLoadingCountries = signal(false);
  readonly isSubmitting = signal(false);

  readonly error = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Formulaire public minimal demandé
  readonly form = {
    CompanyName: '',
    CompanyEmail: '',
    CompanyPhoneNumber: '',
    AdminFirstName: '',
    AdminLastName: '',
    AdminEmail: '',
    AdminPhone: '',
  };

  ngOnInit(): void {
    // Aucun chargement de référentiel nécessaire pour la version minimaliste.
  }

  private validateClientSide(): string | null {
    const f = this.form;
    if (!f.CompanyName.trim()) return 'Le nom de l’entreprise est requis.';
    if (!f.CompanyEmail.trim() || !f.CompanyEmail.includes('@')) return "L'email de l’entreprise est invalide.";
    if (!f.CompanyPhoneNumber.trim()) return 'Le téléphone de l’entreprise est requis.';

    if (!f.AdminFirstName.trim()) return "Le prénom de l'administrateur est requis.";
    if (!f.AdminLastName.trim()) return "Le nom de l'administrateur est requis.";
    if (!f.AdminEmail.trim() || !f.AdminEmail.includes('@'))
      return "L'email de l’administrateur est invalide.";
    if (!f.AdminPhone.trim()) return "Le téléphone de l’administrateur est requis.";

    return null;
  }

  submit(): void {
    this.error.set(null);
    this.successMessage.set(null);

    const clientError = this.validateClientSide();
    if (clientError) {
      this.error.set(clientError);
      return;
    }

    this.isSubmitting.set(true);

    const payload: any = {
      ...this.form,
      // backend utilise `isActive` (lowercase) mais valeur par défaut à true côté DTO.
      isActive: true,
    };

    this.http.post<any>(`${this.apiUrl}/public/signup-company-admin`, payload).subscribe({
      next: (r) => {
        this.isSubmitting.set(false);
        const msg =
          r?.Admin?.Message ??
          "Entreprise créée. Un email d’invitation a été envoyé à l’administrateur.";
        this.successMessage.set(msg);
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

