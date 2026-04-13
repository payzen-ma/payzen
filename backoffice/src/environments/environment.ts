export const environment = {
  production: false,
  apiUrl: 'https://api-test.payzenhr.com/api',
  CALENDARIFIC_API_KEY: 'wH57vFANKLTJzKaSllZOxN0hOMttW3FR',
  entra: {
    clientId: '4d4e0bb5-c180-4513-b8b5-46f6ae4a0b6a',
    authority: 'https://login.microsoftonline.com/eb71cb3b-e338-470a-b9a4-248216338dfd',
    knownAuthorities: ['login.microsoftonline.com'],
    redirectUri: 'http://localhost:50171/auth/callback',
    postLogoutRedirectUri: 'http://localhost:50171/login',
  },
};
