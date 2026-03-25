export const environment = {
  production: false,
  apiUrl: 'http://localhost:5119/api',
  entra: {
    clientId: '4524266d-a29c-4c05-97c9-03e5ee5034ab',
    /** External ID uniquement : sous-domaine + ciamlogin.com (lier l’app au user flow dans le portail). */
    authority: 'https://payzenhr.ciamlogin.com',
    knownAuthorities: ['payzenhr.ciamlogin.com'],
    redirectUri: 'http://localhost:4200/auth/callback',
    postLogoutRedirectUri: 'http://localhost:4200/login',
  }
};
