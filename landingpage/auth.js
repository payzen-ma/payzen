/**
 * URL de l'application Angular.
 * La landing ne construit pas l'URL authorize Entra manuellement
 * pour éviter les erreurs PKCE (AADSTS9002325).
 */
const APP_LOGIN_URL = 'http://localhost:4200/login?autoEntra=1';

/**
 * Redirect vers l'app Angular, qui déclenche ensuite msal.loginRedirect()
 * (PKCE auto géré par MSAL).
 */
function redirectToEntra() {
  window.location.href = APP_LOGIN_URL;
}
  
  /**
   * Fonction scrollCTA conservée pour les ancres internes
   */
  function scrollCTA() {
    document.getElementById('cta')?.scrollIntoView({ behavior: 'smooth' });
  }
  
  /**
   * (Optionnel) Fonction join conservée pour la waitlist email
   * Si vous voulez garder la possibilité de collecter des emails
   * AVANT que l'utilisateur se connecte
   */
  function join(emailId, successId, formId) {
    const email = document.getElementById(emailId)?.value;
    if (!email || !email.includes('@')) {
      alert('Veuillez entrer une adresse email valide.');
      return;
    }
    
    // TODO : Envoyer l'email à votre backend pour la waitlist
    // fetch('/api/waitlist', { method: 'POST', body: JSON.stringify({ email }) });
    
    document.getElementById(formId).style.display = 'none';
    document.getElementById(successId).style.display = 'block';
  }