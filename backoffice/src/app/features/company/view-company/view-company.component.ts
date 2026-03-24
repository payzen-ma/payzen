import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CompanyService } from '../../../services/company.service';
import { Company } from '../../../models/company.model';

@Component({
  selector: 'app-view-company',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './view-company.component.html'
})
export class ViewCompanyComponent implements OnInit {
  companyId: number = 0;
  company: Company | null = null;
  isLoading = false;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private companyService: CompanyService
  ) {}

  ngOnInit() {
    this.companyId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadCompany();
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

  getStatusClass(status: string): string {
    return status === 'active' 
      ? 'bg-green-100 text-green-800' 
      : 'bg-gray-100 text-gray-800';
  }

  getStatusLabel(status: string): string {
    return status === 'active' ? 'Actif' : 'Inactif';
  }
}
