export const environment = {
  production: true,
  apiUrl: 'https://api.payzen.ma/api',
  CALENDARIFIC_API_KEY: '',
  entra: {
    clientId: '4524266d-a29c-4c05-97c9-03e5ee5034ab',
    authority: 'https://payzenhr.ciamlogin.com',
    knownAuthorities: ['payzenhr.ciamlogin.com'],
    redirectUri: 'https://admin.payzen.ma/auth/callback',
    postLogoutRedirectUri: 'https://admin.payzen.ma/login',
  },
};
