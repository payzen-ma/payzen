/**
 * URLs de l'application Angular.
 * La landing ne construit pas l'URL authorize Entra manuellement
 * pour éviter les erreurs PKCE (AADSTS9002325).
 * MSAL dans l'app Angular gère automatiquement PKCE.
 */
// Détection automatique de l'environnement
const ENV_URLS = {
  'test.payzenhr.com': 'https://app-test.payzenhr.com',
  'demo.payzenhr.com': 'https://app-test.payzenhr.com',
  'payzenhr.com': 'https://app.payzenhr.com',
  'www.payzenhr.com': 'https://app.payzenhr.com',
  'localhost': 'http://localhost:4200'
};

const APP_BASE_URL = ENV_URLS[window.location.hostname] || 'http://localhost:4200';

/**
 * Redirige vers l'app Angular qui déclenche msal.loginRedirect()
 * avec prompt 'login' (connexion d'un compte existant).
 */
function redirectToSignIn() {
  window.location.href = `${APP_BASE_URL}/login`;
}

/**
 * Redirige vers l'app Angular qui déclenche msal.loginRedirect()
 * avec prompt 'create' pour forcer le flow Sign-Up Entra.
 */
function redirectToSignUp() {
  // Landing -> login en mode signup (prompt=create), puis callback => /signup/company
  window.location.href = `${APP_BASE_URL}/login?mode=signup`;
}

/**
 * Alias de compatibilité (anciens boutons).
 */
function redirectToEntra() {
  redirectToSignIn();
}

function scrollCTA() {
  document.getElementById('cta')?.scrollIntoView({ behavior: 'smooth' });
}
