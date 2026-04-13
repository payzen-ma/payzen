import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, WritableSignal, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CompanyDocumentDto } from '@app/core/models/company.model';
import { CompanyDocumentService } from '@app/core/services/company-document.service';
import { CompanyService } from '@app/core/services/company.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { FileUploadModule } from 'primeng/fileupload';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-documents-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    DialogModule,
    FileUploadModule,
    InputTextModule,
    ToastModule,
    TagModule,
    TooltipModule,
    SelectModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './documents-tab.component.html'
})
export class DocumentsTabComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly companyService = inject(CompanyService);
  private readonly companyDocumentService = inject(CompanyDocumentService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly contextService = inject(CompanyContextService);
  private readonly translate = inject(TranslateService);
  private contextSub?: Subscription;

  // Forms
  signatoryForm!: FormGroup;
  formSubmitted = false;

  // State
  documents = signal<CompanyDocumentDto[]>([]);
  loading = signal(false);
  uploading = signal(false);
  /** Tracks which image type is currently being uploaded (signature | stamp | logo) */
  uploadingImageType = signal<'signature' | 'stamp' | 'logo' | null>(null);
  signatureUrl = signal<string | null>(null);
  stampUrl = signal<string | null>(null);
  logoUrl = signal<string | null>(null);

  // Upload dialog state
  showUploadDialog = signal(false);
  selectedFile: File | null = null;
  selectedFileName = signal<string | null>(null);
  uploadDocumentType: string | null = null;

  // Document type options for the dropdown
  readonly documentTypes = [
    { label: 'Attestation CNSS', value: 'cnss_attestation' },
    { label: 'Attestation AMO', value: 'amo' },
    { label: 'RIB Bancaire', value: 'rib' },
    { label: 'Registre de Commerce', value: 'rc' },
    { label: 'Patente', value: 'patente' },
    { label: 'Signature', value: 'signature' },
    { label: 'Cachet / Tampon', value: 'stamp' },
    { label: 'Logo', value: 'logo' },
    { label: 'Autre', value: 'other' }
  ];

  /** Object URLs created from blobs — revoked on destroy to avoid memory leaks */
  private blobUrls: string[] = [];

  ngOnInit() {
    this.initForm();
    this.loadData();
    this.contextSub = this.contextService.contextChanged$.subscribe(() => this.loadData());
  }

  ngOnDestroy() {
    this.contextSub?.unsubscribe();
    // Release any blob object URLs created for image previews
    this.blobUrls.forEach(u => URL.revokeObjectURL(u));
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.signatoryForm.get(fieldName);
    return !!(control?.invalid && (control.touched || this.formSubmitted));
  }

  private initForm() {
    this.signatoryForm = this.fb.group({
      signatoryName: ['', [Validators.required, Validators.minLength(2)]],
      signatoryTitle: ['', [Validators.required, Validators.minLength(2)]]
    });
  }

  loadData() {
    const companyId = this.contextService.companyId();
    if (!companyId) return;

    this.loading.set(true);

    // Load signatory data from company service
    this.companyService.getCompany().subscribe({
      next: (company) => this.loadSignatoryData(company),
      error: (err) => alert('Error loading company data: ' + (err?.error?.message ?? err?.message ?? err))
    });

    // Load documents from the dedicated API
    this.companyDocumentService.getByCompany(Number(companyId)).subscribe({
      next: (docs) => {
        this.documents.set(docs);
        this.extractImageUrls(docs);
        this.loading.set(false);
      },
      error: (err) => {
        this.showToast('error',
          this.translate.instant('common.error'),
          this.translate.instant('company.documents.messages.loadError'));
        this.loading.set(false);
      }
    });
  }

  private loadSignatoryData(company: any) {
    this.signatoryForm.patchValue({
      signatoryName: company?.signatoryName || '',
      signatoryTitle: company?.signatoryTitle || ''
    });
  }

  /** Derive signature / stamp / logo preview URLs from the documents list via the download API */
  private extractImageUrls(docs: CompanyDocumentDto[]) {
    const latest = (type: string) =>
      docs.filter(d => d.documentType === type)
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];
    this.loadImagePreview(latest('signature'), this.signatureUrl);
    this.loadImagePreview(latest('stamp'), this.stampUrl);
    this.loadImagePreview(latest('logo'), this.logoUrl);
  }

  /** Fetch a document blob and set an object URL on the given signal */
  private loadImagePreview(doc: CompanyDocumentDto | undefined, urlSignal: WritableSignal<string | null>) {
    if (!doc) {
      urlSignal.set(null);
      return;
    }
    this.companyDocumentService.download(doc.id).subscribe({
      next: (blob) => {
        const objectUrl = URL.createObjectURL(blob);
        this.blobUrls.push(objectUrl);
        urlSignal.set(objectUrl);
      },
      error: () => urlSignal.set(null)
    });
  }

  // ─── Official Images upload ───────────────────────────────────────
  private uploadImageDoc(file: File, type: 'signature' | 'stamp' | 'logo') {
    const companyId = this.contextService.companyId();
    if (!companyId) return;

    const formData = new FormData();
    formData.append('companyId', companyId);
    formData.append('file', file, file.name);
    formData.append('documentType', type);

    this.uploadingImageType.set(type);
    this.companyDocumentService.upload(formData).subscribe({
      next: (doc) => {
        this.documents.update(docs => [doc, ...docs]);
        this.extractImageUrls([doc, ...this.documents()]);
        this.uploadingImageType.set(null);
        this.showToast('success',
          this.translate.instant('common.success'),
          this.translate.instant(`company.documents.messages.${type}Uploaded`));
      },
      error: (err) => {
        this.uploadingImageType.set(null);
        this.showToast('error',
          this.translate.instant('common.error'),
          this.translate.instant('company.documents.messages.uploadError'));
      }
    });
  }

  private removeImageDoc(type: 'signature' | 'stamp' | 'logo') {
    const doc = this.documents()
      .filter(d => d.documentType === type)
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];
    if (!doc) {
      // Nothing on server yet – just clear the local preview
      if (type === 'signature') this.signatureUrl.set(null);
      if (type === 'stamp') this.stampUrl.set(null);
      if (type === 'logo') this.logoUrl.set(null);
      return;
    }
    this.companyDocumentService.delete(doc.id).subscribe({
      next: () => {
        this.documents.update(docs => docs.filter(d => d.id !== doc.id));
        if (type === 'signature') this.signatureUrl.set(null);
        if (type === 'stamp') this.stampUrl.set(null);
        if (type === 'logo') this.logoUrl.set(null);
        this.showToast('info',
          this.translate.instant('common.success'),
          this.translate.instant(`company.documents.messages.${type}Removed`));
      },
      error: (err) => {
        this.showToast('error',
          this.translate.instant('common.error'),
          this.translate.instant('company.documents.messages.deleteError'));
      }
    });
  }

  onUploadSignature(event: { files: File[] }) {
    const file = event.files[0];
    if (file) this.uploadImageDoc(file, 'signature');
  }

  onUploadStamp(event: { files: File[] }) {
    const file = event.files[0];
    if (file) this.uploadImageDoc(file, 'stamp');
  }

  onUploadLogo(event: { files: File[] }) {
    const file = event.files[0];
    if (file) this.uploadImageDoc(file, 'logo');
  }

  onSubmit() {
    this.formSubmitted = true;
    if (this.signatoryForm.invalid) {
      this.signatoryForm.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    const { signatoryName, signatoryTitle } = this.signatoryForm.value;
    this.companyService.updateCompany({ signatoryName, signatoryTitle }).subscribe({
      next: () => {
        this.loading.set(false);
        this.formSubmitted = false;
        this.showToast('success',
          this.translate.instant('common.success'),
          this.translate.instant('company.documents.messages.signatorySaved'));
      },
      error: (err) => {
        this.loading.set(false);
        this.showToast('error',
          this.translate.instant('common.error'),
          this.translate.instant('company.documents.messages.signatorySaveError'));
      }
    });
  }

  removeSignature() { this.removeImageDoc('signature'); }
  removeStamp() { this.removeImageDoc('stamp'); }
  removeLogo() { this.removeImageDoc('logo'); }

  // ─── Upload document dialog ───────────────────────────────────────
  openUploadDialog() {
    this.selectedFile = null;
    this.selectedFileName.set(null);
    this.uploadDocumentType = null;
    this.showUploadDialog.set(true);
  }

  /** Capture the file when user selects it (auto=true, customUpload=true) */
  onFileSelected(event: { files: File[] }) {
    this.selectedFile = event.files[0] ?? null;
    this.selectedFileName.set(this.selectedFile?.name ?? null);
  }

  confirmUpload() {
    if (!this.selectedFile) return;
    const companyId = this.contextService.companyId();
    if (!companyId) return;

    const formData = new FormData();
    formData.append('companyId', companyId);
    formData.append('file', this.selectedFile, this.selectedFile.name);
    if (this.uploadDocumentType) {
      formData.append('documentType', this.uploadDocumentType);
    }

    this.uploading.set(true);
    this.companyDocumentService.upload(formData).subscribe({
      next: (doc) => {
        this.documents.update(docs => [doc, ...docs]);
        this.showUploadDialog.set(false);
        this.uploading.set(false);
        this.showToast('success',
          this.translate.instant('common.success'),
          this.translate.instant('company.documents.messages.documentUploaded'));
      },
      error: (err) => {
        this.uploading.set(false);
        this.showToast('error',
          this.translate.instant('common.error'),
          this.translate.instant('company.documents.messages.uploadError'));
      }
    });
  }

  // ─── Document actions ─────────────────────────────────────────────
  downloadDocument(doc: CompanyDocumentDto) {
    this.companyDocumentService.download(doc.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = doc.name;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.showToast('error',
          this.translate.instant('common.error'),
          this.translate.instant('company.documents.messages.downloadError'));
      }
    });
  }

  deleteDocument(doc: CompanyDocumentDto) {
    this.confirmationService.confirm({
      message: this.translate.instant('company.documents.messages.deleteConfirm', { name: doc.name }),
      header: this.translate.instant('common.confirm'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.companyDocumentService.delete(doc.id).subscribe({
          next: () => {
            this.documents.update(docs => docs.filter(d => d.id !== doc.id));
            this.showToast('success',
              this.translate.instant('common.success'),
              this.translate.instant('company.documents.messages.deleteSuccess'));
          },
          error: (err) => {
            this.showToast('error',
              this.translate.instant('common.error'),
              this.translate.instant('company.documents.messages.deleteError'));
          }
        });
      }
    });
  }

  // ─── Helpers ──────────────────────────────────────────────────────
  getDocumentTypeLabel(type: string | null): string {
    if (!type) return '—';
    return this.documentTypes.find(t => t.value === type)?.label ?? type;
  }

  getFileIcon(name: string): string {
    const ext = name.split('.').pop()?.toLowerCase() ?? '';
    switch (ext) {
      case 'pdf': return 'pi pi-file-pdf text-red-500';
      case 'jpg': case 'jpeg': case 'png': return 'pi pi-image text-blue-400';
      case 'doc': case 'docx': return 'pi pi-file text-blue-600';
      case 'xls': case 'xlsx': return 'pi pi-file text-green-600';
      default: return 'pi pi-file text-gray-400';
    }
  }

  private showToast(severity: 'success' | 'error' | 'info', summary: string, detail: string) {
    this.messageService.add({ severity, summary, detail, life: 4000 });
  }
}

