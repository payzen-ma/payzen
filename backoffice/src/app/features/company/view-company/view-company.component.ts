import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CompanyService } from '../../../services/company.service';
import { RoleService } from '../../../services/role.service';
import { InvitationService } from '../../../services/invitation.service';
import { ToastService } from '../../../shared/toast/toast.service';
import { Company } from '../../../models/company.model';
import { Role } from '../../../models/role.model';

@Component({
  selector: 'app-view-company',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './view-company.component.html'
})
export class ViewCompanyComponent implements OnInit {
  companyId: number = 0;
  company: Company | null = null;
  roles: Role[] = [];
  inviteEmail = '';
  inviteRoleId: number | null = null;
  isInviting = false;
  isLoading = false;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private companyService: CompanyService,
    private roleService: RoleService,
    private invitationService: InvitationService,
    private toast: ToastService
  ) {}

  ngOnInit() {
    this.companyId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadCompany();
    this.loadRoles();
  }

  loadCompany() {
    this.isLoading = true;
    this.error = null;
    this.companyService.getCompanyById(this.companyId).subscribe({
      next: (data) => {
        this.company = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Erreur lors du chargement de l\'entreprise';
        this.isLoading = false;
        console.error('Error loading company:', err);
      }
    });
  }

  loadRoles() {
    this.roleService.getAllRoles().subscribe({
      next: (data) => {
        this.roles = data;
        this.inviteRoleId = this.detectAdminRoleId(data);
      },
      error: (err) => {
        console.error('Error loading roles:', err);
        this.toast.error('Impossible de charger les rôles pour l’invitation.');
      }
    });
  }

  inviteAdmin() {
    if (!this.companyId) {
      this.toast.error('Entreprise invalide.');
      return;
    }

    const email = this.inviteEmail.trim();
    if (!email || !this.isValidEmail(email)) {
      this.toast.error('Veuillez saisir une adresse email valide.');
      return;
    }

    if (!this.inviteRoleId) {
      this.toast.error('Rôle admin introuvable. Vérifiez la configuration des rôles.');
      return;
    }

    this.isInviting = true;
    this.invitationService.inviteAdmin({
      email,
      companyId: this.companyId,
      roleId: this.inviteRoleId
    }).subscribe({
      next: () => {
        this.isInviting = false;
        this.inviteEmail = '';
        this.toast.success('Invitation admin envoyée avec succès.');
      },
      error: (err) => {
        this.isInviting = false;
        const message =
          err?.error?.Message ||
          err?.error?.message ||
          'Échec de l’envoi de l’invitation admin.';
        this.toast.error(message);
        console.error('Error inviting admin:', err);
      }
    });
  }

  private detectAdminRoleId(roles: Role[]): number | null {
    const adminRole = roles.find((r) => {
      const name = (r.name || '').toLowerCase().trim();
      return name === 'admin' || name === 'administrator';
    });
    return adminRole?.id ?? null;
  }

  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

  getStatusClass(status: string): string {
    return status === 'active' 
      ? 'bg-green-100 text-green-800' 
      : 'bg-gray-100 text-gray-800';
  }

  getStatusLabel(status: string): string {
    return status === 'active' ? 'Actif' : 'Inactif';
  }
}
