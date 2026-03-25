export const environment = {
  production: false,
  apiUrl: 'http://localhost:5119/api',
  CALENDARIFIC_API_KEY: 'wH57vFANKLTJzKaSllZOxN0hOMttW3FR',
  entra: {
    clientId: '4524266d-a29c-4c05-97c9-03e5ee5034ab',
    authority: 'https://payzenhr.ciamlogin.com',
    knownAuthorities: ['payzenhr.ciamlogin.com'],
    redirectUri: 'http://localhost:50171/auth/callback',
    postLogoutRedirectUri: 'http://localhost:50171/login',
  },
};
