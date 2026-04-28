import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TextInputComponent } from '../../../shared/components/text-input/text-input.component';
import { ModalComponent } from '../../../shared/modal/modal.component';
// Removed unused component imports: SelectInputComponent, PrimaryButtonComponent
import { CompanyService } from '../../../services/company.service';
import { City, Country } from '../../../models/company.model';

@Component({
  selector: 'app-create-company',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TextInputComponent,
    ModalComponent
  ],
  templateUrl: './create-company.component.html'
})
export class CreateCompanyComponent implements OnInit {
  private companyService = inject(CompanyService);
  // Wizard step
  currentStep = 1;
  totalSteps = 2;

  // Form data - Company
  companyName = '';
  email = '';
  phoneCountryCode = '+212';
  phoneNumber = '';
  address = '';
  city = '';
  country = '';
  cnss = '';
  isAccountingFirm = false;
  licence = '';

  // Form data - Admin Account
  adminFirstName = '';
  adminLastName = '';
  adminEmail = '';
  adminPhone = '';
  adminDateOfBirth = '';

  // Options
  countryCodes = [
    { code: '+212', label: '🇲🇦 +212 (Maroc)' },
    { code: '+33', label: '🇫🇷 +33 (France)' },
    { code: '+34', label: '🇪🇸 +34 (Espagne)' },
    { code: '+49', label: '🇩🇪 +49 (Allemagne)' }
  ];

  countries: Country[] = [];
  cities: City[] = [];
  filteredCities: City[] = [];
  selectedCountryId: number | null = null;
  showCityDropdown = false;
  selectedCityId: number | null = null;

  // Success modal
  showSuccessModal = false;
  createdAdmin: any = null;
  isSubmitting = false;

  // Notifications (toasts)
  notifications: { id: number; type: 'error' | 'success' | 'info'; message: string }[] = [];
  private nextNotificationId = 1;

  licences = [
    { value: 'starter', label: 'Starter', description: 'Jusqu\'\u00e0 10 employ\u00e9s', price: '500 MAD/mois' },
    { value: 'professional', label: 'Professional', description: 'Jusqu\'\u00e0 50 employ\u00e9s', price: '1500 MAD/mois' },
    { value: 'enterprise', label: 'Enterprise', description: 'Jusqu\'\u00e0 200 employ\u00e9s', price: '3500 MAD/mois' },
    { value: 'unlimited', label: 'Unlimited', description: 'Employ\u00e9s illimit\u00e9s', price: 'Sur devis' }
  ];
  ngOnInit() {
    this.loadFormData();
  }

  loadFormData() {
    this.companyService.getFormData().subscribe({
      next: (data) => {
        this.cities = data.cities;
        this.countries = data.countries;

        // Set Morocco as default country
        const morocco = this.countries.find(c => c.name.toLowerCase() === 'maroc');
        if (morocco) {
          this.selectedCountryId = morocco.id;
          this.country = morocco.name;
          this.filterCitiesByCountry();
        }
      },
      error: (error) => {
      }
    });
  }

  onCountryChange() {
    const selectedCountry = this.countries.find(c => c.name === this.country);
    this.selectedCountryId = selectedCountry ? selectedCountry.id : null;
    this.city = ''; // Reset city when country changes
    this.filterCitiesByCountry();
  }

  filterCitiesByCountry() {
    if (this.selectedCountryId) {
      this.filteredCities = this.cities.filter(c => c.countryId === this.selectedCountryId);
    } else {
      this.filteredCities = this.cities;
    }
  }

  onCityInput(event: Event) {
    const input = (event.target as HTMLInputElement).value;
    this.city = input;

    if (input.length > 0) {
      this.filteredCities = this.cities.filter(c =>
        c.countryId === this.selectedCountryId &&
        c.name.toLowerCase().includes(input.toLowerCase())
      );
      this.showCityDropdown = this.filteredCities.length > 0;
    } else {
      this.filterCitiesByCountry();
      this.showCityDropdown = false;
    }
    this.selectedCityId = null;
  }

  selectCity(city: City) {
    this.city = city.name;
    this.selectedCityId = city.id;
    this.showCityDropdown = false;
  }

  onCityBlur() {
    // Delay to allow click on dropdown item
    setTimeout(() => {
      this.showCityDropdown = false;
    }, 200);
  }
  nextStep() {
    if (this.currentStep < this.totalSteps) {
      this.currentStep++;
    }
  }

  previousStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  onSubmit() {
    if (this.isSubmitting) return;

    this.isSubmitting = true;

    const selectedCountry = this.countries.find(c => c.name === this.country);

    const companyData: any = {
      companyName: this.companyName,
      email: this.email,
      phoneNumber: this.phoneNumber,
      countryPhoneCode: this.phoneCountryCode,
      companyAddress: this.address,
      countryId: selectedCountry?.id || 1,
      cnssNumber: this.cnss,
      isCabinetExpert: this.isAccountingFirm,
      adminFirstName: this.adminFirstName,
      adminLastName: this.adminLastName,
      adminEmail: this.adminEmail,
      adminDateOfBirth: this.adminDateOfBirth,
      adminPhone: this.adminPhone
    };

    // Add cityId if existing city selected, otherwise add cityName for new city
    if (this.selectedCityId) {
      companyData.cityId = this.selectedCityId;
    } else if (this.city) {
      companyData.cityName = this.city;
    }

    this.companyService.createCompany(companyData).subscribe({
      next: (response: any) => {

        // Transform admin data to handle both PascalCase and camelCase
        const admin = response.admin || response.Admin;
        this.createdAdmin = {
          email: admin?.email || admin?.Email,
          firstName: admin?.firstName || admin?.FirstName,
          lastName: admin?.lastName || admin?.LastName,
          country: response.countryName || response.country || this.country,
          message: admin?.message || admin?.Message
        };
        this.showSuccessModal = true;
        this.isSubmitting = false;
      },
      error: (error) => {
        this.showNotification('Erreur lors de la création de l\'entreprise: ' + (error.error?.message || error.message), 'error');
        this.isSubmitting = false;
      }
    });
  }

  closeSuccessModal() {
    this.showSuccessModal = false;
    // Optionally redirect or reset form
    window.location.href = '/companies';
  }

  copyToClipboard(text: string) {
    navigator.clipboard.writeText(text).then(() => {
      this.showNotification('Copié dans le presse-papier!', 'success');
    });
  }

  showNotification(message: string, type: 'error' | 'success' | 'info' = 'info') {
    const id = this.nextNotificationId++;
    this.notifications.push({ id, type, message });
    setTimeout(() => this.dismissNotification(id), 5000);
  }

  dismissNotification(id: number) {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }
}
