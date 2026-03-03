import { Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { AVAILABLE_LANGUAGES, DEFAULT_LANGUAGE, LANGUAGE_STORAGE_KEY, Language } from './translation.config';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  // Signal to track current language
  currentLanguage = signal<Language>(DEFAULT_LANGUAGE);
  
  // Signal to track RTL state
  isRTL = signal<boolean>(false);
  
  // Available languages
  readonly availableLanguages = AVAILABLE_LANGUAGES;

  constructor(private translate: TranslateService) {
    this.initializeLanguage();
  }

  /**
   * Initialize language from localStorage or use default
   */
  private initializeLanguage(): void {
    const savedLanguage = this.getStoredLanguage();
    const languageToUse = savedLanguage || this.getBrowserLanguage() || DEFAULT_LANGUAGE;
    
    this.setLanguage(languageToUse);
  }

  /**
   * Get stored language from localStorage
   */
  private getStoredLanguage(): Language | null {
    const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY);
    if (stored && this.isValidLanguage(stored)) {
      return stored as Language;
    }
    return null;
  }

  /**
   * Get browser language if it's supported
   */
  private getBrowserLanguage(): Language | null {
    const browserLang = this.translate.getBrowserLang();
    if (browserLang && this.isValidLanguage(browserLang)) {
      return browserLang as Language;
    }
    return null;
  }

  /**
   * Check if language code is valid
   */
  private isValidLanguage(lang: string): boolean {
    return AVAILABLE_LANGUAGES.includes(lang as Language);
  }

  /**
   * Set the current language
   */
  setLanguage(lang: Language): void {
    if (!this.isValidLanguage(lang)) {
      console.warn(`Invalid language: ${lang}. Using default: ${DEFAULT_LANGUAGE}`);
      lang = DEFAULT_LANGUAGE;
    }

    this.translate.use(lang);
    this.currentLanguage.set(lang);
    localStorage.setItem(LANGUAGE_STORAGE_KEY, lang);
    
    // Update HTML lang attribute for accessibility
    document.documentElement.lang = lang;
    
    // Set RTL direction for Arabic
    const isRtl = lang === 'ar';
    this.isRTL.set(isRtl);
    
    if (isRtl) {
      document.documentElement.dir = 'rtl';
      document.body.classList.add('rtl');
    } else {
      document.documentElement.dir = 'ltr';
      document.body.classList.remove('rtl');
    }
  }

  /**
   * Get current language
   */
  getCurrentLanguage(): Language {
    return this.currentLanguage();
  }

  /**
   * Get translation for a key
   */
  translate$(key: string | string[], params?: object) {
    return this.translate.stream(key, params);
  }

  /**
   * Get instant translation for a key
   */
  instant(key: string | string[], params?: object): string {
    return this.translate.instant(key, params);
  }

  /**
   * Check if current language is RTL
   */
  isRtlLanguage(): boolean {
    return this.isRTL();
  }
}

