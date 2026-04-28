import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Company, CompanyCreateByExpertDto } from '@app/core/models/company.model';
import { CompanyService } from '@app/core/services/company.service';
import { AuthService } from '@app/core/services/auth.service';
import { EmployeeService, CityLookupOption, CountryLookupOption } from '@app/core/services/employee.service';
import { SelectFieldComponent } from '@app/shared/components/form-controls/select-field';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-client-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonModule,
    InputTextModule,
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
  private translate = inject(TranslateService);

  @Input() mode: 'create' | 'edit' = 'create';
  @Input() company?: Company;
  @Output() save = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  form!: FormGroup;
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  // City search
  cities = signal<CityLookupOption[]>([]);
  countries = signal<CountryLookupOption[]>([]);
  citiesLoading = signal<boolean>(false);
  countriesLoading = signal<boolean>(false);

  ngOnInit(): void {
    this.initForm();
    this.loadInitialCountries();
    this.loadInitialCities();
    if (this.mode === 'edit' && this.company) {
      this.patchForm();
    }
  }

  private loadInitialCountries(): void {
    this.countriesLoading.set(true);
    this.employeeService.searchCountries('').subscribe({
      next: (countries) => {
        this.countries.set(countries);
        this.countriesLoading.set(false);

        // Default to Morocco when creating.
        if (this.mode === 'create') {
          const morocco = countries.find(c => c.label.toLowerCase() === 'maroc');
          if (morocco) {
            this.form.patchValue({ countryId: morocco.id });
          } else if (countries.length > 0) {
            this.form.patchValue({ countryId: countries[0].id });
          }
        }

        if (this.mode === 'edit' && this.company?.country) {
          const selected = countries.find(c => c.label.toLowerCase() === this.company!.country.toLowerCase());
          if (selected) {
            this.form.patchValue({ countryId: selected.id });
          }
        }
      },
      error: () => {
        this.countriesLoading.set(false);
      }
    });
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

  onCountrySearch(query: string): void {
    this.countriesLoading.set(true);
    this.employeeService.searchCountries(query).subscribe({
      next: (countries) => {
        this.countries.set(countries);
        this.countriesLoading.set(false);
      },
      error: () => {
        this.countriesLoading.set(false);
      }
    });
  }

  onCreateCity(cityName: string): void {
    this.citiesLoading.set(true);
    const selectedCountryId = this.form.get('countryId')?.value as number | null;
    this.employeeService.createCity(cityName, selectedCountryId ?? undefined).subscribe({
      next: (newCity) => {
        // Add the new city to the list
        this.cities.update(cities => [...cities, newCity]);
        // Set it as the selected value
        this.form.patchValue({ city: newCity.label });
        this.citiesLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(this.translate.instant('errors.createFailed'));
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
      countryId: [null, [Validators.required]],
      city: ['', [Validators.required]],
      ice: [''],
      cnss: ['', [Validators.required]],
      rc: [''],
      if: [''],
      rib: ['']
    });

    this.form.get('countryId')?.valueChanges.subscribe(() => {
      this.form.patchValue({ city: '' }, { emitEvent: false });
    });
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

  getFilteredCities(): CityLookupOption[] {
    const selectedCountryId = this.form.get('countryId')?.value as number | null;
    if (!selectedCountryId) {
      return this.cities();
    }
    return this.cities().filter(city => city.countryId === selectedCountryId);
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
      this.errorMessage.set(this.translate.instant('errors.createFailed'));
      this.isLoading.set(false);
      return;
    }

    const formValue = this.form.value;
    // Le backend expert n'utilise plus ces champs pour créer un admin,
    // mais le DTO actuel les exige encore : on envoie des valeurs techniques.
    const generatedAdminEmail = `no-admin+${Date.now()}@payzen.local`;

    const dto: CompanyCreateByExpertDto = {
      CompanyName: formValue.companyName,
      CompanyEmail: formValue.email,
      CompanyPhoneNumber: formValue.phone,
      CompanyAddress: formValue.address,
      CityName: formValue.city,
      CountryId: Number(formValue.countryId),
      CnssNumber: formValue.cnss,
      ManagedByCompanyId: Number(expertCompanyId),
      AdminFirstName: 'N/A',
      AdminLastName: 'N/A',
      AdminEmail: generatedAdminEmail,
      AdminPhone: '',
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
        let msg = this.translate.instant('errors.createFailed');

        if (err.status === 409) {
          msg = this.translate.instant('errors.createFailed');
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
        this.errorMessage.set('Failed to update company. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  onCancel(): void {
    this.cancel.emit();
  }
}

