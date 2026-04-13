export const environment = {
    production: false,
    apiUrl: 'https://api-test.payzenhr.com/api',
    entra: {
        clientId: '6961f3db-ba3e-4803-938b-14285b09b391',
        authority: 'https://payzenhrtest.ciamlogin.com/b80bbebd-7733-41d0-9528-9947785f4262',
        knownAuthorities: ['payzenhrtest.ciamlogin.com'],
        redirectUri: 'https://app-test.payzenhr.com/auth/callback',
        postLogoutRedirectUri: 'https://demo.payzenhr.com',
        scopes: [
            'openid',
            'profile',
            'email',
            'offline_access',
            'api://f87d2b7e-4b64-4a92-9583-080d3d7bd829/access_as_user'
        ]
    }
};

