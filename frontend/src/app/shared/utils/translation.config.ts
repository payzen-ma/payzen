// Available languages
export const AVAILABLE_LANGUAGES = ['en', 'ar', 'fr'] as const;
export type Language = typeof AVAILABLE_LANGUAGES[number];

// Default language
export const DEFAULT_LANGUAGE: Language = 'fr';

// Language storage key
export const LANGUAGE_STORAGE_KEY = 'payzen_language';

