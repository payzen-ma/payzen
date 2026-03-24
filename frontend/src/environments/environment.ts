export const environment = {
  production: false,
  apiUrl: 'http://localhost:5119/api',
  entra: {
    clientId: '0bd1e09a-2b59-4b49-a802-a91f70851e38',
    authority: 'https://payzenhr.ciamlogin.com/payzenhr.onmicrosoft.com',
    redirectUri: 'http://localhost:4200/auth/callback',
    postLogoutRedirectUri: 'http://localhost:4200/login',
  }
};
