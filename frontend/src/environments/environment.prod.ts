export const environment = {
  production: true,
  apiUrl: 'https://api.payzen.ma/api',
  apiVersion: 'v1',
  entra: {
    clientId: '',
    authority: '',
    knownAuthorities: ['payzenhr.ciamlogin.com'],
    scopes: ['openid', 'profile', 'email', 'offline_access']
  }
};
