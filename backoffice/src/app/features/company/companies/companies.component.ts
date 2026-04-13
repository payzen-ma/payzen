import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Company } from '../../../models/company.model';
import { CompanyService } from '../../../services/company.service';

@Component({
  selector: 'app-companies',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './companies.component.html'
})
export class CompaniesComponent implements OnInit {
  companies: Company[] = [];
  filteredCompanies: Company[] = [];
  paginatedCompanies: Company[] = [];
  searchTerm = '';
  filterType: 'all' | 'expert' | 'standard' = 'all';
  showFilters = false;
  isLoading = false;
  error: string | null = null;
  Math = Math; // Expose Math to template
  updatingCompanyIds: number[] = [];

  // Sorting
  sortColumn: string = 'companyName';
  sortDirection: 'asc' | 'desc' = 'asc';

  // Pagination
  currentPage = 1;
  itemsPerPage = 5;
  totalPages = 1;

  constructor(
    private companyService: CompanyService,
    private elementRef: ElementRef
  ) { }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    const clickedInside = this.elementRef.nativeElement.contains(target);

    if (!clickedInside && this.showFilters) {
      this.showFilters = false;
    }
  }

  isUpdating(company: Company): boolean {
    return this.updatingCompanyIds.includes(company.id);
  }

  toggleActive(company: Company) {
    const current = (company as any).isActive;
    // If isActive is undefined, fall back to status
    const newIsActive = typeof current === 'boolean' ? !current : (company.status !== 'active');

    // mark updating
    this.updatingCompanyIds.push(company.id);

    this.companyService.patchCompany(company.id, { isActive: newIsActive }).subscribe({
      next: (updated) => {
        // update local arrays: replace company with updated one
        const replace = (arr: Company[]) => {
          const idx = arr.findIndex(c => c.id === updated.id);
          if (idx !== -1) arr[idx] = updated;
        };
        replace(this.companies);
        replace(this.filteredCompanies);
        replace(this.paginatedCompanies);
        // refresh filters/pagination views
        this.applyFilters();
        // remove updating flag
        this.updatingCompanyIds = this.updatingCompanyIds.filter(id => id !== updated.id);
      },
      error: (err) => {
        this.updatingCompanyIds = this.updatingCompanyIds.filter(id => id !== company.id);
      }
    });
  }

  ngOnInit() {
    this.loadCompanies();
  }

  loadCompanies() {
    this.isLoading = true;
    this.error = null;
    this.companyService.getAllCompanies().subscribe({
      next: (data) => {
        this.companies = data;
        this.filteredCompanies = data;
        this.totalPages = Math.ceil(data.length / this.itemsPerPage);
        this.updatePaginatedData();
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Erreur lors du chargement des entreprises';
        this.isLoading = false;
      }
    });
  }

  onSearch() {
    this.applyFilters();
  }

  applyFilters() {
    let filtered = this.companies;

    // Filter by type
    if (this.filterType === 'expert') {
      filtered = filtered.filter(c => c.isCabinetExpert);
    } else if (this.filterType === 'standard') {
      filtered = filtered.filter(c => !c.isCabinetExpert);
    }

    // Filter by search term
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(company =>
        company.companyName.toLowerCase().includes(term) ||
        company.email.toLowerCase().includes(term) ||
        company.cityName.toLowerCase().includes(term)
      );
    }

    // Apply sorting
    filtered = this.sortData(filtered);

    this.filteredCompanies = filtered;
    this.totalPages = Math.ceil(filtered.length / this.itemsPerPage);
    this.currentPage = 1; // Reset to first page when filters change
    this.updatePaginatedData();
  }

  sortBy(column: string) {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.applyFilters();
  }

  sortData(data: Company[]): Company[] {
    return [...data].sort((a, b) => {
      let aValue: any;
      let bValue: any;

      switch (this.sortColumn) {
        case 'companyName':
          aValue = a.companyName?.toLowerCase() || '';
          bValue = b.companyName?.toLowerCase() || '';
          break;
        case 'email':
          aValue = a.email?.toLowerCase() || '';
          bValue = b.email?.toLowerCase() || '';
          break;
        case 'cityName':
          aValue = a.cityName?.toLowerCase() || '';
          bValue = b.cityName?.toLowerCase() || '';
          break;
        case 'cnssNumber':
          aValue = a.cnssNumber || '';
          bValue = b.cnssNumber || '';
          break;
        case 'createdAt':
          aValue = new Date(a.createdAt).getTime();
          bValue = new Date(b.createdAt).getTime();
          break;
        case 'isCabinetExpert':
          aValue = a.isCabinetExpert ? 1 : 0;
          bValue = b.isCabinetExpert ? 1 : 0;
          break;
        default:
          return 0;
      }

      if (aValue < bValue) {
        return this.sortDirection === 'asc' ? -1 : 1;
      }
      if (aValue > bValue) {
        return this.sortDirection === 'asc' ? 1 : -1;
      }
      return 0;
    });
  }

  setFilterType(type: 'all' | 'expert' | 'standard') {
    this.filterType = type;
    this.applyFilters();
    this.showFilters = false; // Close menu after selection
  }

  toggleFilters() {
    this.showFilters = !this.showFilters;
  }

  getSortIcon(column: string): string {
    if (this.sortColumn !== column) return '';
    return this.sortDirection === 'asc' ? '↑' : '↓';
  }

  updatePaginatedData() {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    const endIndex = startIndex + this.itemsPerPage;
    this.paginatedCompanies = this.filteredCompanies.slice(startIndex, endIndex);
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.updatePaginatedData();
    }
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.updatePaginatedData();
    }
  }

  previousPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updatePaginatedData();
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxPages = 5; // Maximum number of page buttons to show

    if (this.totalPages <= maxPages) {
      for (let i = 1; i <= this.totalPages; i++) {
        pages.push(i);
      }
    } else {
      // Always show first page
      pages.push(1);

      // Show pages around current page
      const start = Math.max(2, this.currentPage - 1);
      const end = Math.min(this.totalPages - 1, this.currentPage + 1);

      if (start > 2) {
        pages.push(-1); // Represents ellipsis
      }

      for (let i = start; i <= end; i++) {
        pages.push(i);
      }

      if (end < this.totalPages - 1) {
        pages.push(-1); // Represents ellipsis
      }

      // Always show last page
      pages.push(this.totalPages);
    }

    return pages;
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
