export const environment = {
  production: true,
  apiUrl: 'https://api.payzen.ma/api',
  CALENDARIFIC_API_KEY: '',
  entra: {
    clientId: '4524266d-a29c-4c05-97c9-03e5ee5034ab',
    // Entra ID Workforce (backoffice interne)
    authority: 'https://login.microsoftonline.com/eb71cb3b-e338-470a-b9a4-248216338dfd',
    knownAuthorities: ['login.microsoftonline.com'],
    redirectUri: 'https://admin.payzen.ma/auth/callback',
    postLogoutRedirectUri: 'https://admin.payzen.ma/login',
  },
};
