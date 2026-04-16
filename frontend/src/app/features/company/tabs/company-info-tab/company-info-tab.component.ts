import { Component, OnInit, inject, signal, computed, OnDestroy, WritableSignal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { InputTextModule } from 'primeng/inputtext';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';
import { ButtonModule } from 'primeng/button';
import { FileUploadModule } from 'primeng/fileupload';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { CompanyService } from '@app/core/services/company.service';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { CompanyDocumentService } from '@app/core/services/company-document.service';
import { Company } from '@app/core/models/company.model';
import { EditableFieldComponent } from '@app/shared/components/editable-field/editable-field.component';
import { ReadonlyFieldComponent } from '@app/shared/components/readonly-field/readonly-field.component';
import { Subscription } from 'rxjs';

interface FieldConfig {
  id: string;
  key: keyof Company;
  label: string;
  fullWidth?: boolean;
  type?: string;
}

@Component({
  selector: 'app-company-info-tab',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    InputTextModule,
    InputGroupModule,
    InputGroupAddonModule,
    ButtonModule,
    FileUploadModule,
    ToastModule,
    EditableFieldComponent,
    ReadonlyFieldComponent
  ],
  providers: [MessageService],
  templateUrl: './company-info-tab.component.html',
  styleUrls: ['./company-info-tab.component.css']
})
export class CompanyInfoTabComponent implements OnInit, OnDestroy {
  private readonly companyService = inject(CompanyService);
  private readonly messageService = inject(MessageService);
  private readonly authService = inject(AuthService);
  private readonly contextService = inject(CompanyContextService);
  private readonly companyDocumentService = inject(CompanyDocumentService);
  private readonly translate = inject(TranslateService);
  private contextSub?: Subscription;

  // Computed signals
  isAdmin = computed(() => this.authService.isAdmin());

  // State
  loading = signal(false);
  company = signal<Company | null>(null);
  citySuggestions = signal<string[]>([]);
  logoUrl = signal<string | null>(null);

  /** Blob object URLs to revoke on destroy */
  private logoBlobUrl: string | null = null;

  // Field configurations for DRY template
  readonly legalFields: FieldConfig[] = [
    { id: 'legalName', key: 'legalName', label: 'company.info.raisonSociale' },
    { id: 'ice', key: 'ice', label: 'company.info.ice' },
    { id: 'if', key: 'if', label: 'company.info.identifiantFiscal' },
    { id: 'rc', key: 'rc', label: 'company.info.rc' },
    { id: 'patente', key: 'patente', label: 'company.info.patente' },
    { id: 'foundingDate', key: 'foundingDate', label: 'company.info.foundingDate', type: 'date' },
    { id: 'legalForm', key: 'legalForm', label: 'company.info.legalForm' },
    { id: 'cnss', key: 'cnss', label: 'company.info.cnss' },
    { id: 'rib', key: 'rib', label: 'company.info.rib' },
    { id: 'website', key: 'website', label: 'company.info.website' }
  ];

  readonly editableFields: FieldConfig[] = [
    { id: 'email', key: 'email', label: 'company.info.email' },
    { id: 'phone', key: 'phone', label: 'company.info.phone' },
    { id: 'address', key: 'address', label: 'company.info.address', fullWidth: true },
    { id: 'city', key: 'city', label: 'company.info.city', type: 'autocomplete' },
    { id: 'matriculeTemplate', key: 'matriculeTemplate', label: 'company.info.matriculeTemplate' },

  ];

  ngOnInit() {
    this.loadCompanyData();

    // Subscribe to context changes to reload data
    this.contextSub = this.contextService.contextChanged$.subscribe(() => {
      this.loadCompanyData();
    });
  }

  ngOnDestroy() {
    if (this.contextSub) {
      this.contextSub.unsubscribe();
    }
    if (this.logoBlobUrl) {
      URL.revokeObjectURL(this.logoBlobUrl);
    }
  }

  searchCities(event: any) {
    this.companyService.searchCities(event.query).subscribe(suggestions => {
      this.citySuggestions.set(suggestions);
    });
  }

  /** Get field value from company object */
  getFieldValue(key: keyof Company): string {
    const val = this.company()?.[key] as any;
    if (key === 'foundingDate') {
      if (!val) return '';
      // If it's a Date, format to yyyy-mm-dd for <input type="date"> value
      const d = val instanceof Date ? val : new Date(val);
      if (isNaN(d.getTime())) return '';
      return d.toISOString().split('T')[0];
    }
    return (val as string) ?? '';
  }

  private loadLogoPreview() {
    const companyId = this.contextService.companyId();
    if (!companyId) return;
    this.companyDocumentService.getByCompany(+companyId).subscribe({
      next: (docs) => {
        const latest = docs
          .filter(d => d.documentType === 'logo')
          .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];
        if (!latest) { this.logoUrl.set(null); return; }
        this.companyDocumentService.download(latest.id).subscribe({
          next: (blob) => {
            if (this.logoBlobUrl) URL.revokeObjectURL(this.logoBlobUrl);
            this.logoBlobUrl = URL.createObjectURL(blob);
            this.logoUrl.set(this.logoBlobUrl);
          },
          error: () => this.logoUrl.set(null)
        });
      },
      error: () => this.logoUrl.set(null)
    });
  }

  private loadCompanyData() {
    this.loading.set(true);
    this.companyService.getCompany().subscribe({
      next: (data) => {
        this.company.set(data);
        this.loading.set(false);
        this.loadLogoPreview();
      },
      error: (err) => {
        this.showToast(
          'error',
          this.translate.instant('common.error'),
          this.translate.instant('company.info.messages.loadError')
        );
        this.loading.set(false);
      }
    });
  }

  onUpload(event: { files: File[] }) {
    const file = event.files[0];
    if (!file) return;

    const companyId = this.contextService.companyId();
    if (!companyId) return;

    const formData = new FormData();
    formData.append('companyId', companyId);
    formData.append('file', file, file.name);
    formData.append('documentType', 'logo');

    this.companyDocumentService.upload(formData).subscribe({
      next: () => {
        this.showToast(
          'success',
          this.translate.instant('common.success'),
          this.translate.instant('company.info.messages.logoUpdated')
        );
        this.loadLogoPreview();
      },
      error: () => {
        this.showToast(
          'error',
          this.translate.instant('common.error'),
          this.translate.instant('company.info.messages.logoUpdateError')
        );
      }
    });
  }

  updateField(field: keyof Company, value: string) {
    this.loading.set(true);

    const currentCompany = this.company();
    if (!currentCompany) {
      this.loading.set(false);
      return;
    }
    // Convert foundingDate string (from input) to a Date object
    const payloadValue = field === 'foundingDate' ? (value ? new Date(value) : undefined) : value;

    const updatePayload: Partial<Company> = {
      id: currentCompany.id,
      [field]: payloadValue as any
    };

    // Optimistic UI: apply change locally immediately so user sees the updated value
    const previous = { ...currentCompany };
    this.company.set({ ...currentCompany, [field]: payloadValue as any });

    this.companyService.updateCompany(updatePayload).subscribe({
      next: (updatedCompany) => {
        // Merge server response with previous but prefer the locally-updated value
        const merged = this.mergeCompany(previous, updatedCompany, { [field]: payloadValue as any });
        this.company.set(merged);
        this.showToast(
          'success',
          this.translate.instant('common.success'),
          this.translate.instant('company.info.messages.fieldUpdated')
        );
        this.loading.set(false);
      },
      error: (err) => {
        // Revert optimistic update on error
        this.company.set(previous);
        this.showToast(
          'error',
          this.translate.instant('common.error'),
          this.translate.instant('company.info.messages.fieldUpdateError')
        );
        this.loading.set(false);
      }
    });
  }

  // Merge server-updated company into previous local company state.
  // Preserve previous values for keys where server returned undefined or empty string
  private mergeCompany(prev: Company, server: Company, overrides: Partial<Company> = {}): Company {
    const result: any = { ...prev };
    if (!server) return { ...result, ...overrides } as Company;

    // Apply server values when present
    Object.keys(server).forEach((k) => {
      const val = (server as any)[k];
      if (val !== undefined && val !== '') {
        result[k] = val;
      }
    });

    // Apply overrides (local updates) last so they take precedence
    Object.keys(overrides).forEach((k) => {
      const val = (overrides as any)[k];
      if (val !== undefined) {
        result[k] = val;
      }
    });

    return result as Company;
  }

  private showToast(severity: 'success' | 'error' | 'info', summary: string, detail: string) {
    this.messageService.add({ severity, summary, detail, life: 4000 });
  }
}
