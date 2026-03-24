import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HolidayService } from '../../../services/holiday.service';
import { HolidayReadDto, HolidayCreateDto, HolidayUpdateDto, HolidayScope } from '../../../models/holiday.model';
import { ConfirmService } from '../../../shared/confirm/confirm.service';
import { ModalComponent } from '../../../shared/modal/modal.component';
import { Country } from '../../../models/company.model';
import { CompanyService } from '../../../services/company.service';
import { environment } from '../../../../environments/environment';
import { firstValueFrom } from 'rxjs';
import { HOLIDAY_TRANSLATIONS, normalizeHolidayName } from '../../../i18n/holidays-translations';

@Component({
  selector: 'app-holidays',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  templateUrl: './holidays.component.html'
})
export class HolidaysComponent implements OnInit {
  holidays: HolidayReadDto[] = [];
  countries: Country[] = [];
  holidayTypes: string[] = [];
  loading = false;
  // cached Calendarific API key entered by the user during this session
  calendarificApiKey: string | null = null;

  // Modal state
  modalVisible = false;
  modalMode: 'create' | 'edit' = 'create';
  modalModel: any = null;

  // Filter state
  selectedYear: number = new Date().getFullYear();
  selectedCountryId?: number;
  showInactive = false;
  // When true, automatically import missing holidays for a year when the year filter changes
  autoImportOnYearChange = true;

  HolidayScope = HolidayScope; // For template access

  constructor(
    private holidayService: HolidayService,
    private companyService: CompanyService,
    private confirmService: ConfirmService
  ) {}

  ngOnInit(): void {
    this.loadCountries();
    this.loadHolidayTypes();
    this.refresh();
  }

  loadCountries(): void {
    this.companyService.getFormData().subscribe({
      next: (data) => {
        this.countries = data.countries;
      },
      error: (err) => console.error('Failed to load countries', err)
    });
  }

  loadHolidayTypes(): void {
    this.holidayService.getHolidayTypes().subscribe({
      next: (types) => {
        // Use API types if available, otherwise fallback to defaults
        this.holidayTypes = types.length > 0 
          ? types 
          : ['National', 'Religieux', 'Légal', 'Culturel', 'Autre'];
      },
      error: (err) => {
        console.error('Failed to load holiday types', err);
        // Fallback to default types
        this.holidayTypes = ['National', 'Religieux', 'Légal', 'Culturel', 'Autre'];
      }
    });
  }

  refresh(): void {
    this.loading = true;
    this.holidayService.getGlobalHolidays(this.selectedCountryId, this.showInactive, this.selectedYear).subscribe({
      next: (list) => {
        this.holidays = list;
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load holidays', err);
        this.loading = false;
      }
    });
  }

  openModal(mode: 'create' | 'edit', item?: HolidayReadDto): void {
    this.modalMode = mode;
    if (mode === 'edit' && item) {
      this.modalModel = {
        id: item.id,
        nameFr: item.nameFr,
        nameAr: item.nameAr,
        nameEn: item.nameEn,
        holidayDate: item.holidayDate,
        description: item.description || '',
        countryId: item.countryId,
        scope: item.scope,
        holidayType: item.holidayType,
        isMandatory: item.isMandatory,
        isPaid: item.isPaid,
        isRecurring: item.isRecurring,
        recurrenceRule: item.recurrenceRule || '',
        year: item.year,
        affectPayroll: item.affectPayroll,
        affectAttendance: item.affectAttendance,
        isActive: item.isActive
      };
    } else {
      this.modalModel = {
        nameFr: '',
        nameAr: '',
        nameEn: '',
        holidayDate: '',
        description: '',
        countryId: this.selectedCountryId || (this.countries.length > 0 ? this.countries[0].id : undefined),
        scope: HolidayScope.Global,
        holidayType: 'National',
        isMandatory: true,
        isPaid: true,
        isRecurring: false,
        recurrenceRule: '',
        year: this.selectedYear,
        affectPayroll: true,
        affectAttendance: true,
        isActive: true
      };
    }
    this.modalVisible = true;
  }

  saveFromModal(): void {
    const m = this.modalModel;
    if (!m || !m.nameFr || !m.nameAr || !m.nameEn || !m.holidayDate || !m.countryId) {
      alert('Veuillez remplir tous les champs obligatoires');
      return;
    }

    if (this.modalMode === 'create') {
      const createDto: HolidayCreateDto = {
        nameFr: m.nameFr,
        nameAr: m.nameAr,
        nameEn: m.nameEn,
        holidayDate: m.holidayDate,
        description: m.description || undefined,
        companyId: undefined, // Global holiday (NULL)
        countryId: m.countryId,
        scope: HolidayScope.Global,
        holidayType: m.holidayType,
        isMandatory: m.isMandatory,
        isPaid: m.isPaid,
        isRecurring: m.isRecurring,
        recurrenceRule: m.recurrenceRule || undefined,
        year: m.year,
        affectPayroll: m.affectPayroll,
        affectAttendance: m.affectAttendance,
        isActive: m.isActive
      };

      this.holidayService.createHoliday(createDto).subscribe({
        next: () => {
          this.modalVisible = false;
          this.refresh();
        },
        error: (err) => {
          console.error('Failed to create holiday', err);
          alert('Erreur lors de la création du jour férié');
        }
      });
    } else {
      const updateDto: HolidayUpdateDto = {
        nameFr: m.nameFr,
        nameAr: m.nameAr,
        nameEn: m.nameEn,
        holidayDate: m.holidayDate,
        description: m.description || undefined,
        countryId: m.countryId,
        scope: m.scope,
        holidayType: m.holidayType,
        isMandatory: m.isMandatory,
        isPaid: m.isPaid,
        isRecurring: m.isRecurring,
        recurrenceRule: m.recurrenceRule || undefined,
        year: m.year,
        affectPayroll: m.affectPayroll,
        affectAttendance: m.affectAttendance,
        isActive: m.isActive
      };

      this.holidayService.updateHoliday(m.id, updateDto).subscribe({
        next: () => {
          this.modalVisible = false;
          this.refresh();
        },
        error: (err) => {
          console.error('Failed to update holiday', err);
          alert('Erreur lors de la mise à jour du jour férié');
        }
      });
    }
  }

  deleteHoliday(id: number, name: string): void {
    this.confirmService.confirm(
      `Êtes-vous sûr de vouloir supprimer le jour férié "${name}" ? Cette action est irréversible.`
    ).then(confirmed => {
      if (confirmed) {
        this.holidayService.deleteHoliday(id).subscribe({
          next: () => this.refresh(),
          error: (err) => {
            console.error('Failed to delete holiday', err);
            alert('Erreur lors de la suppression du jour férié');
          }
        });
      }
    });
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { 
      day: '2-digit', 
      month: 'long', 
      year: 'numeric' 
    });
  }

  onFilterChange(): void {
    // Check backend for holidays for the selected year; if none found and auto-import
    // is enabled, attempt to import from Calendarific (using env key or prompt).
    const year = this.selectedYear || new Date().getFullYear();
    this.loading = true;
    this.holidayService.getGlobalHolidays(this.selectedCountryId, this.showInactive, year).subscribe({
      next: async (list) => {
        this.holidays = list;
        this.loading = false;

        if ((!list || list.length === 0) && this.autoImportOnYearChange) {
          // use API key only from Angular environment or cached value (no prompting)
          const envKey = environment.CALENDARIFIC_API_KEY || (import.meta as any)?.env?.VITE_CALENDARIFIC_API_KEY;
          console.log('Auto-import: using env key?', !!envKey, 'cached key?', !!this.calendarificApiKey);
          const apiKey = this.calendarificApiKey || envKey;
          if (!apiKey) {
            console.warn('No Calendarific API key found in env or cache; skipping auto-import.');
            return;
          }

          // determine country code from selected country; if missing, fall back to first available country
          let countryCode: string | undefined;
          if (this.selectedCountryId) {
            const c = this.countries.find(x => x.id === this.selectedCountryId);
            countryCode = c?.code || undefined;
          } else if (this.countries && this.countries.length > 0) {
            // fallback: use the first country in the list
            const c = this.countries[0];
            countryCode = c.code;
            this.selectedCountryId = c.id;
            console.log('Auto-import: no country selected, falling back to', c.code);
          }

          if (!countryCode) {
            console.warn('No country available; skipping auto-import for year', year);
            return;
          }

          // Validate country code: ensure it's an ISO-3166 alpha-2 (two letters).
          let codeToUse = (countryCode || '').toString();
          if (!/^[A-Za-z]{2}$/.test(codeToUse)) {
            // Prompt the user to supply a proper country code when auto-import would otherwise use an invalid code
            const promptValue = window.prompt(`Le code pays trouvé ("${codeToUse}") n'est pas un code ISO-3166 alpha-2. Entrez le code pays à utiliser (ex: MA) :`, codeToUse || 'MA');
            if (!promptValue) {
              console.warn('Auto-import cancelled by user due to invalid country code');
              return;
            }
            codeToUse = promptValue.trim();
          }

          await this.performImport(apiKey, codeToUse.toUpperCase(), year);
        }
      },
      error: (err) => {
        console.error('Failed to load holidays', err);
        this.loading = false;
      }
    });
  }

  private async performImport(apiKey: string, countryCode: string, year: number): Promise<void> {
    this.loading = true;
    return new Promise((resolve) => {
      console.log('performImport: calling fetchExternalHolidays', { apiKeyProvided: !!apiKey, countryCode, year });
      this.holidayService.fetchExternalHolidays(apiKey, countryCode, year).subscribe({
        next: async (items) => {
          console.log('performImport: fetched external items count=', items?.length, 'items=', items);
          if (!items || items.length === 0) {
            this.loading = false;
            console.warn('performImport: external API returned zero items for', countryCode, year);
            alert('Aucun jour férié trouvé par l\'API externe.');
            resolve();
            return;
          }

          const national = items.filter((it: any) => {
            if (it.primary_type && typeof it.primary_type === 'string') return it.primary_type.toLowerCase().includes('national');
            if (Array.isArray(it.type)) return it.type.some((t: string) => t.toLowerCase().includes('national'));
            return false;
          });
          console.log('performImport: national items count=', national.length, 'nationalItems=', national);

          const countryId = this.selectedCountryId ?? this.countries.find(x => (x.code || '').toUpperCase() === countryCode.toUpperCase())?.id;
          if (!countryId) {
            this.loading = false;
            alert('Impossible de retrouver l\'ID du pays local. Sélectionnez un pays dans le filtre.');
            resolve();
            return;
          }

          let created = 0;
          for (const it of national) {
            try {
              const iso: string = it.date?.iso || it.date;
              const dateOnly = iso.split('T')[0];
              // Prefer explicit translations if provided by the external API.
              const srcName: string = it.name || '';
              const translations = it.translations || it.name_translations || it.nameTranslations || {};
              const hasTranslations = translations && Object.keys(translations).length > 0;

              // Map i18n translations from the external item into our DTO language fields
              // Try common translation keys first, otherwise fall back to the source name
              const pick = (obj: any, ...keys: string[]) => {
                if (!obj) return undefined;
                for (const k of keys) {
                  if (obj[k]) return obj[k];
                }
                return undefined;
              };

              // Prefer our curated i18n mapping when available (better quality translations)
              const key = normalizeHolidayName(srcName);
              const mapped = key ? (HOLIDAY_TRANSLATIONS[key] || null) : null;

              const nameEn = mapped?.en
                || pick(translations, 'en', 'english', 'en_US', 'en_GB')
                || it.name_en || it.nameEn || srcName;

              const nameFr = mapped?.fr
                || pick(translations, 'fr', 'french', 'fr_FR')
                || it.name_fr || it.nameFr || srcName;

              const nameAr = mapped?.ar
                || pick(translations, 'ar', 'arabic')
                || it.name_ar || it.nameAr || srcName;

              const dto: HolidayCreateDto = {
                nameFr: nameFr || srcName || 'Jour férié',
                nameAr: nameAr || srcName || 'يوم عطلة',
                nameEn: nameEn || srcName || 'Holiday',
                holidayDate: dateOnly,
                description: it.description || undefined,
                companyId: undefined,
                countryId: countryId,
                scope: HolidayScope.Global,
                holidayType: it.primary_type || (Array.isArray(it.type) ? it.type[0] : 'National'),
                isMandatory: true,
                isPaid: true,
                isRecurring: false,
                recurrenceRule: undefined,
                year: year,
                affectPayroll: true,
                affectAttendance: true,
                isActive: true
              };

              console.log('performImport: creating holiday DTO=', dto);
              const res = await firstValueFrom(this.holidayService.createHoliday(dto).pipe());
              console.log('performImport: createHoliday response=', res);
              created++;
            } catch (err: any) {
              console.warn('Import item failed or already exists', (err as any)?.message ?? err, err);
            }
          }

          this.loading = false;
          this.refresh();
          console.log('performImport: created count=', created);
          alert(`${created} jours fériés importés.`);
          resolve();
        },
        error: (err) => {
          console.error('Failed to fetch external holidays', err);
          this.loading = false;
          alert('Échec de la récupération depuis Calendarific: ' + (err?.message || err));
          resolve();
        }
      });
    });
  }

  async importFromCalendarific(): Promise<void> {
    // Prefer API key from Angular environment or Vite env var
    const envKey = environment.CALENDARIFIC_API_KEY || (import.meta as any)?.env?.VITE_CALENDARIFIC_API_KEY;
    let apiKey = this.calendarificApiKey || envKey;
    if (!apiKey) {
      const key = window.prompt('Entrez la clé API Calendarific:');
      if (!key) return;
      apiKey = key.trim();
      this.calendarificApiKey = apiKey;
    }

    // determine country code
    let countryCode: string | undefined;
    if (this.selectedCountryId) {
      const c = this.countries.find(x => x.id === this.selectedCountryId);
      countryCode = c?.code || undefined;
    }
    if (!countryCode) {
      countryCode = window.prompt('Entrez le code pays (ex: MA) :', 'MA') || undefined;
    }
    if (!countryCode) {
      alert('Code pays requis pour l\'import.');
      return;
    }

    const year = this.selectedYear || new Date().getFullYear();
    await this.performImport(apiKey, countryCode.toUpperCase(), year);
  }
}
