import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CompanyService } from '../../../services/company.service';
import { City, Country } from '../../../models/company.model';

@Component({
  selector: 'app-edit-company',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './edit-company.component.html'
})
export class EditCompanyComponent implements OnInit {
  companyId: number = 0;
  
  // Form data
  companyName = '';
  email = '';
  phoneCountryCode = '+212';
  phoneNumber = '';
  address = '';
  city = '';
  country = '';
  cnss = '';
  status = 'active';
  isAccountingFirm = false;
  licence = '';

  // Legal & Fiscal Information
  iceNumber = '';
  ifNumber = '';
  rcNumber = '';
  legalForm = '';
  foundingDate = '';

  // Options
  countryCodes = [
    { code: '+212', label: '🇲🇦 +212 (Maroc)' },
    { code: '+33', label: '🇫🇷 +33 (France)' },
    { code: '+34', label: '🇪🇸 +34 (Espagne)' },
    { code: '+49', label: '🇩🇪 +49 (Allemagne)' }
  ];

  // Will be populated from API
  countries: Country[] = [];
  allCities: City[] = [];
  cities: City[] = []; // filtered cities for selected country
  selectedCountry: Country | null = null;
  selectedCity: City | null = null;
  showCityDropdown = false;
  filteredCities: City[] = [];
  statuses = [
    { value: 'active', label: 'Actif' },
    { value: 'inactive', label: 'Inactif' }
  ];

  formesJuridiques = ['SARL', 'SA', 'SAS', 'SARL AU', 'SNC', 'SCS', 'SCA', 'Entreprise individuelle'];

  licences = [
    { value: 'starter', label: 'Starter', description: 'Jusqu\'à 10 employés', price: '500 MAD/mois' },
    { value: 'professional', label: 'Professional', description: 'Jusqu\'à 50 employés', price: '1500 MAD/mois' },
    { value: 'enterprise', label: 'Enterprise', description: 'Jusqu\'à 200 employés', price: '3500 MAD/mois' },
    { value: 'unlimited', label: 'Unlimited', description: 'Employés illimités', price: 'Sur devis' }
  ];

  isLoading = false;
  isSubmitting = false;
  errorMessage: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private companyService: CompanyService
  ) {}

  ngOnInit() {
    this.companyId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadFormData();
  }

  loadFormData() {
    this.companyService.getFormData().subscribe({
      next: (data) => {
        this.countries = data.countries || [];
        this.allCities = data.cities || [];
        // If no country selected yet, default to Morocco if present, otherwise first country
        if (!this.country) {
          const defaultCountry = this.countries.find(c => /maroc|morocco/i.test(c.name)) || this.countries[0];
          if (defaultCountry) {
            this.country = defaultCountry.name;
            this.selectedCountry = defaultCountry;
            this.onCountryChange(defaultCountry);
          }
        } else {
          // If company already has a country, find the matching Country object
          const found = this.countries.find(c => c.name === this.country || String(c.id) === String(this.country));
          if (found) {
            this.selectedCountry = found;
            this.onCountryChange(found);
          }
        }

        // Now load company data (after form data to allow selection)
        this.loadCompany();
      },
      error: (err) => {
        console.error('Erreur lors du chargement des données de formulaire:', err);
        // still attempt to load company even if form data fails
        this.loadCompany();
      }
    });
  }

  onCountryChange(countryIdOrName: Country | number | string | null) {
    // Accept either a Country object or id/name
    let countryId: number | undefined;
    if (!countryIdOrName) {
      this.cities = this.allCities.slice();
      this.selectedCity = null;
      return;
    }
    if (typeof countryIdOrName === 'object') {
      countryId = (countryIdOrName as Country).id;
      this.selectedCountry = countryIdOrName as Country;
      this.country = this.selectedCountry.name;
    } else if (typeof countryIdOrName === 'number') {
      countryId = countryIdOrName;
      this.selectedCountry = this.countries.find(c => c.id === countryId) || null;
      this.country = this.selectedCountry ? this.selectedCountry.name : this.country;
    } else {
      const found = this.countries.find(c => c.name === countryIdOrName || String(c.id) === countryIdOrName);
      countryId = found ? found.id : undefined;
      this.selectedCountry = found || null;
      this.country = this.selectedCountry ? this.selectedCountry.name : this.country;
    }
    if (countryId == null) {
      this.cities = this.allCities.slice();
      this.selectedCity = null;
      return;
    }
    this.cities = this.allCities.filter(city => city.countryId === countryId);
    // Reset suggestions to cities of this country
    this.filteredCities = this.cities.slice();
    // If selectedCity exists but doesn't belong to this country, clear it
    if (this.selectedCity && this.selectedCity.countryId !== countryId) {
      this.selectedCity = null;
      this.city = '';
    }
  }

  onCityChange(cityObj: City | null) {
    if (!cityObj) {
      this.selectedCity = null;
      this.city = '';
      return;
    }
    this.selectedCity = cityObj;
    this.city = cityObj.name;
    this.showCityDropdown = false;
  }

  filterCities(search: string) {
    const q = (search || '').toLowerCase().trim();
    const countryId = this.selectedCountry ? this.selectedCountry.id : undefined;
    if (!q) {
      // show all cities for the selected country (or all)
      this.filteredCities = countryId == null ? this.allCities.slice() : this.allCities.filter(c => c.countryId === countryId);
      this.showCityDropdown = this.filteredCities.length > 0;
      return;
    }
    this.filteredCities = this.allCities.filter(c => {
      if (countryId != null && c.countryId !== countryId) return false;
      return c.name.toLowerCase().includes(q);
    });
    this.showCityDropdown = this.filteredCities.length > 0;
  }

  onCityInput() {
    this.filterCities(this.city);
    // keep dropdown visible while typing
    this.showCityDropdown = this.filteredCities.length > 0;
  }

  onCityBlur() {
    // Delay hiding to allow click selection (mousedown) to register
    setTimeout(() => {
      this.showCityDropdown = false;
    }, 150);
  }

  selectCity(c: City) {
    this.selectedCity = c;
    this.city = c.name;
    this.showCityDropdown = false;
  }

  loadCompany() {
    this.isLoading = true;
    this.companyService.getCompanyById(this.companyId).subscribe({
      next: (c) => {
        this.companyName = c.companyName || '';
        this.email = c.email || '';
        this.phoneCountryCode = c.countryCode || this.phoneCountryCode;
        this.phoneNumber = c.phoneNumber ? String(c.phoneNumber) : '';
        this.address = c.companyAddress || '';
        this.city = c.cityName || this.city;
        this.country = c.countryName || this.country;
        if (c.countryName) {
          const foundCountry = this.countries.find(x => x.name === c.countryName);
          if (foundCountry) {
            this.selectedCountry = foundCountry;
            this.onCountryChange(foundCountry);
          }
        }
        if (c.cityName) {
          const foundCity = this.allCities.find(x => x.name === c.cityName);
          if (foundCity) {
            this.selectedCity = foundCity;
            this.city = foundCity.name;
          }
        }
        this.cnss = c.cnssNumber || '';
        this.status = (c.status as any) || this.status;
        this.isAccountingFirm = !!c.isCabinetExpert;

        this.licence = c.licence || this.licence;

        // Legal & Fiscal
        this.iceNumber = c.iceNumber || '';
        this.ifNumber = c.ifNumber || '';
        this.rcNumber = c.rcNumber || '';
        this.legalForm = c.legalForm || '';
        this.foundingDate = c.foundingDate ? c.foundingDate.split('T')[0] : '';
        console.log('foundingDate loaded:', this.foundingDate);

        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur lors du chargement de la société', err);
        this.errorMessage = 'Impossible de charger la société.';
        this.isLoading = false;
      }
    });
  }

  onSubmit() {
    this.isSubmitting = true;
    this.errorMessage = null;

    const payload: any = {
      companyName: this.companyName,
      email: this.email || null,
      phoneNumber: this.phoneNumber || '',
      countryPhoneCode: this.phoneCountryCode || null,
      companyAddress: this.address || null,
      cityName: this.city || null,
      countryName: this.country || null,
      cnssNumber: this.cnss,
      status: this.status,
      isCabinetExpert: this.isAccountingFirm,
      licence: this.licence,
      countryId: this.selectedCountry ? this.selectedCountry.id : null,

      // Legal & Fiscal
      iceNumber: this.iceNumber,
      ifNumber: this.ifNumber,
      rcNumber: this.rcNumber,
      legalForm: this.legalForm,
      foundingDate: this.foundingDate,
    };

    console.log('Submitting company update:', payload);
    this.companyService.patchCompany(this.companyId, payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/view-company', this.companyId]);
      },
      error: (err) => {
        console.error('Erreur lors de la mise à jour de la société', err);
        this.errorMessage = 'Erreur lors de la mise à jour.';
        this.isSubmitting = false;
      }
    });
  }

  onCancel() {
    this.router.navigate(['/view-company', this.companyId]);
  }
}
