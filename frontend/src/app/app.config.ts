import { ApplicationConfig, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { importProvidersFrom } from '@angular/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { camelCaseInterceptor } from './core/interceptors/camelcase.interceptor';
import { routes } from './app.routes';
import { initializeMsal } from './core/config/msal.config';


import { providePrimeNG } from 'primeng/config';
import { PayZenTheme } from '../assets/themes/payzen-theme';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([authInterceptor])
    ),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([authInterceptor, camelCaseInterceptor])),
    providePrimeNG({
      theme: {
        preset: PayZenTheme,
        options: {
          darkModeSelector: false,  // Disable dark mode detection
          cssLayer: {
            name: 'primeng',
            order: 'tailwind-base, primeng, tailwind-utilities'
          }
        }
      }
    }),
    importProvidersFrom(
      TranslateModule.forRoot({
        fallbackLang: 'fr'
      })
    ),
    provideTranslateHttpLoader({
      prefix: '/assets/i18n/',
      suffix: '.json'
    }),
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: () => () => initializeMsal()
    }
  ],
};
