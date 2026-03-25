/**
 * URLs de l'application Angular.
 * La landing ne construit pas l'URL authorize Entra manuellement
 * pour éviter les erreurs PKCE (AADSTS9002325).
 * MSAL dans l'app Angular gère automatiquement PKCE.
 */
const APP_BASE_URL = 'http://localhost:4200';

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
