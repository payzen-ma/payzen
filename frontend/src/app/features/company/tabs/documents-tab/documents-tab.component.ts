import { Component, OnInit, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { FileUploadModule } from 'primeng/fileupload';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { CompanyService } from '@app/core/services/company.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { CompanyDocuments } from '@app/core/models/company.model';
import { Subscription } from 'rxjs';

interface DocumentRow {
  name: string;
  type: string;
  url: string | null;
  status: 'uploaded' | 'missing';
  date?: Date;
}

interface ExpectedDocument {
  key: string;
  label: string;
}

@Component({
  selector: 'app-documents-tab',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    DialogModule,
    FileUploadModule,
    InputTextModule,
    ToastModule,
    TagModule,
    TooltipModule
  ],
  providers: [MessageService],
  templateUrl: './documents-tab.component.html'
})
export class DocumentsTabComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly companyService = inject(CompanyService);
  private readonly messageService = inject(MessageService);
  private readonly contextService = inject(CompanyContextService);
  private readonly translate = inject(TranslateService);
  private contextSub?: Subscription;

  // Forms
  signatoryForm!: FormGroup;
  formSubmitted = false;

  // State
  documents = signal<DocumentRow[]>([]);
  loading = signal(false);
  signatureUrl = signal<string | null>(null);
  stampUrl = signal<string | null>(null);
  selectedDocType = signal<string | null>(null);

  // Document configuration
  private readonly expectedDocs: ExpectedDocument[] = [
    { key: 'cnss_attestation', label: 'company.documents.cnss_attestation' },
    { key: 'amo', label: 'company.documents.amo' },
    { key: 'rib', label: 'company.info.rib' },
    { key: 'rc', label: 'company.info.rc' },
    { key: 'patente', label: 'company.info.patente' }
  ];

  ngOnInit() {
    this.initForm();
    this.loadData();
    
    // Subscribe to context changes
    this.contextSub = this.contextService.contextChanged$.subscribe(() => {
      this.loadData();
    });
  }

  ngOnDestroy() {
    if (this.contextSub) {
      this.contextSub.unsubscribe();
    }
  }

  /** Check if a form field is invalid and should show error */
  isFieldInvalid(fieldName: string): boolean {
    const control = this.signatoryForm.get(fieldName);
    return !!(control?.invalid && (control.touched || this.formSubmitted));
  }

  /** Get status badge classes based on document status */
  getStatusClasses(status: 'uploaded' | 'missing'): string {
    return status === 'uploaded'
      ? 'bg-green-100 text-green-700'
      : 'bg-amber-100 text-amber-700';
  }

  private initForm() {
    this.signatoryForm = this.fb.group({
      signatoryName: ['', [Validators.required, Validators.minLength(2)]],
      signatoryTitle: ['', [Validators.required, Validators.minLength(2)]]
    });
  }

  loadData() {
    this.loading.set(true);
    this.companyService.getCompany().subscribe({
      next: (company) => {
        this.documents.set(this.mapDocuments(company.documents));
        this.loadSignatoryData(company);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading company data:', err);
        this.showToast(
          'error',
          this.translate.instant('common.error'),
          this.translate.instant('company.documents.messages.loadError')
        );
        this.loading.set(false);
      }
    });
  }

  private loadSignatoryData(company: any) {
    if (company.signatory) {
      this.signatoryForm.patchValue({
        signatoryName: company.signatory.name,
        signatoryTitle: company.signatory.title
      });
      this.signatureUrl.set(company.signatory.signatureUrl);
      this.stampUrl.set(company.signatory.stampUrl);
    }
  }

  private mapDocuments(docs: CompanyDocuments): DocumentRow[] {
    return this.expectedDocs.map(doc => {
      const url = (docs as unknown as Record<string, string | undefined>)[doc.key];
      return {
        name: doc.label,
        type: doc.key,
        url: url || null,
        status: url ? 'uploaded' : 'missing',
        date: url ? new Date() : undefined
      };
    });
  }

  onUploadSignature(event: { files: File[] }) {
    const file = event.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => this.signatureUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
    this.showToast(
      'success',
      this.translate.instant('common.success'),
      this.translate.instant('company.documents.messages.signatureUploaded')
    );
  }

  onUploadStamp(event: { files: File[] }) {
    const file = event.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => this.stampUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
    this.showToast(
      'success',
      this.translate.instant('common.success'),
      this.translate.instant('company.documents.messages.stampUploaded')
    );
  }

  onSubmit() {
    this.formSubmitted = true;
    if (this.signatoryForm.invalid) {
      this.signatoryForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    // Mock save - replace with actual API call
    setTimeout(() => {
      this.loading.set(false);
      this.formSubmitted = false;
      this.showToast(
        'success',
        this.translate.instant('common.success'),
        this.translate.instant('company.documents.messages.signatorySaved')
      );
    }, 1000);
  }

  onDocumentUpload(event: { files: File[] }) {
    const docType = this.selectedDocType();
    if (!docType) return;

    this.documents.update(docs =>
      docs.map(d =>
        d.type === docType
          ? { ...d, status: 'uploaded' as const, url: 'mock-url.pdf', date: new Date() }
          : d
      )
    );
    this.showToast(
      'success',
      this.translate.instant('common.success'),
      this.translate.instant('company.documents.messages.documentUploaded')
    );
  }

  download(doc: DocumentRow) {
    if (doc.url) {
      window.open(doc.url, '_blank', 'noopener,noreferrer');
    }
  }

  removeSignature() {
    this.signatureUrl.set(null);
    this.showToast(
      'info',
      this.translate.instant('common.success'),
      this.translate.instant('company.documents.messages.signatureRemoved')
    );
  }

  removeStamp() {
    this.stampUrl.set(null);
    this.showToast(
      'info',
      this.translate.instant('common.success'),
      this.translate.instant('company.documents.messages.stampRemoved')
    );
  }

  private showToast(severity: 'success' | 'error' | 'info', summary: string, detail: string) {
    this.messageService.add({ severity, summary, detail, life: 4000 });
  }
}

