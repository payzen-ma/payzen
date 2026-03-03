import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { Company, CompanyCreateByExpertDto } from '@app/core/models/company.model';
import { CompanyService } from '@app/core/services/company.service';
import { AuthService } from '@app/core/services/auth.service';
import { EmployeeService, CityLookupOption } from '@app/core/services/employee.service';
import { SelectFieldComponent } from '@app/shared/components/form-controls/select-field';

@Component({
  selector: 'app-client-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonModule,
    InputTextModule,
    CheckboxModule,
    SelectFieldComponent
  ],
  templateUrl: './client-form.component.html',
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ClientFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private companyService = inject(CompanyService);
  private authService = inject(AuthService);
  private employeeService = inject(EmployeeService);

  @Input() mode: 'create' | 'edit' = 'create';
  @Input() company?: Company;
  @Output() save = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  form!: FormGroup;
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  
  // City search
  cities = signal<CityLookupOption[]>([]);
  citiesLoading = signal<boolean>(false);

  ngOnInit(): void {
    this.initForm();
    this.loadInitialCities();
    if (this.mode === 'edit' && this.company) {
      this.patchForm();
    }
  }

  private loadInitialCities(): void {
    this.citiesLoading.set(true);
    this.employeeService.searchCities('').subscribe({
      next: (cities) => {
        this.cities.set(cities);
        this.citiesLoading.set(false);
      },
      error: () => {
        this.citiesLoading.set(false);
      }
    });
  }

  onCitySearch(query: string): void {
    this.citiesLoading.set(true);
    this.employeeService.searchCities(query).subscribe({
      next: (cities) => {
        this.cities.set(cities);
        this.citiesLoading.set(false);
      },
      error: () => {
        this.citiesLoading.set(false);
      }
    });
  }

  onCreateCity(cityName: string): void {
    this.citiesLoading.set(true);
    this.employeeService.createCity(cityName).subscribe({
      next: (newCity) => {
        // Add the new city to the list
        this.cities.update(cities => [...cities, newCity]);
        // Set it as the selected value
        this.form.patchValue({ city: newCity.label });
        this.citiesLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set('Failed to create city. Please try again.');
        this.citiesLoading.set(false);
      }
    });
  }

  private initForm(): void {
    this.form = this.fb.group({
      // Company Details
      companyName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required]],
      address: ['', [Validators.required]],
      city: ['', [Validators.required]],
      ice: ['', [Validators.required]],
      cnss: ['', [Validators.required]],
      rc: [''],
      if: [''],
      rib: [''],
      
      // Admin Details (Only for Create)
      adminFirstName: [''],
      adminLastName: [''],
      adminEmail: [''],
      adminPhone: [''],
      generatePassword: [true],
      adminPassword: ['']
    });

    if (this.mode === 'create') {
      this.addAdminValidators();
    }
  }

  private addAdminValidators(): void {
    const adminFields = ['adminFirstName', 'adminLastName', 'adminEmail', 'adminPhone'];
    adminFields.forEach(field => {
      this.form.get(field)?.setValidators([Validators.required]);
      this.form.get(field)?.updateValueAndValidity();
    });
    
    this.form.get('adminEmail')?.addValidators([Validators.email]);
  }

  private patchForm(): void {
    if (!this.company) return;

    this.form.patchValue({
      companyName: this.company.legalName,
      email: this.company.email,
      phone: this.company.phone,
      address: this.company.address,
      city: this.company.city,
      ice: this.company.ice,
      cnss: this.company.cnss,
      rc: this.company.rc,
      if: this.company.if,
      rib: this.company.rib
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    if (this.mode === 'create') {
      this.createCompany();
    } else {
      this.updateCompany();
    }
  }

  private createCompany(): void {
    const expertCompanyId = this.authService.currentUser()?.companyId;
    
    if (!expertCompanyId) {
      this.errorMessage.set('Expert company ID not found');
      this.isLoading.set(false);
      return;
    }

    const formValue = this.form.value;
    
    const dto: CompanyCreateByExpertDto = {
      CompanyName: formValue.companyName,
      CompanyEmail: formValue.email,
      CompanyPhoneNumber: formValue.phone,
      CompanyAddress: formValue.address,
      CityName: formValue.city,
      CountryId: 1, // Default to Morocco
      CnssNumber: formValue.cnss,
      ManagedByCompanyId: Number(expertCompanyId),
      AdminFirstName: formValue.adminFirstName,
      AdminLastName: formValue.adminLastName,
      AdminEmail: formValue.adminEmail,
      AdminPhone: formValue.adminPhone,
      GeneratePassword: formValue.generatePassword,
      AdminPassword: formValue.adminPassword,
      IceNumber: formValue.ice,
      IfNumber: formValue.if,
      RcNumber: formValue.rc,
      RibNumber: formValue.rib,
      IsCabinetExpert: false
    };

    this.companyService.createCompanyByExpert(dto).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.save.emit();
      },
      error: (err) => {
        console.error('Error creating company:', err);
        let msg = 'Failed to create company. Please try again.';
        
        if (err.status === 409) {
          msg = 'A company with this ICE, CNSS, or Email already exists, or the Admin Email is already in use.';
          if (typeof err.error === 'string') {
              msg = err.error;
          } else if (err.error?.message) {
              msg = err.error.message;
          }
        } else if (err.error) {
          if (err.error.errors) {
            // Handle .NET validation errors
            msg = Object.values(err.error.errors).flat().join(', ');
          } else if (err.error.message) {
            msg = err.error.message;
          } else if (typeof err.error === 'string') {
            msg = err.error;
          }
        }
        
        this.errorMessage.set(msg);
        this.isLoading.set(false);
      }
    });
  }

  private updateCompany(): void {
    if (!this.company) return;

    const formValue = this.form.value;
    
    const updateData: Partial<Company> = {
      id: this.company.id,
      legalName: formValue.companyName,
      email: formValue.email,
      phone: formValue.phone,
      address: formValue.address,
      city: formValue.city,
      ice: formValue.ice,
      cnss: formValue.cnss,
      rc: formValue.rc,
      if: formValue.if,
      rib: formValue.rib
    };

    this.companyService.updateCompany(updateData).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.save.emit();
      },
      error: (err) => {
        console.error('Error updating company:', err);
        this.errorMessage.set('Failed to update company. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  onCancel(): void {
    this.cancel.emit();
  }
}

