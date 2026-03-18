# Spécifications : Workflows Authentification SaaS (Mars 2026)

## Principes Généraux
- Aucun mot de passe stocké côté SaaS.
- Provisionnement top-down : Backoffice SaaS → Admin Company → Users.
- Authentification 100% déléguée à Microsoft Entra External ID.
- Aucun self-signup (création par opérateur backoffice uniquement).
- 1 email = 1 company.

## Types de Company & Configuration
- **Type A** : Microsoft 365 / Azure AD (SSO natif).
- **Type A.1** : IdP externe (Google, SAML/OIDC).
- **Type A.2** : AD on-premise (Azure AD Connect).
- **Type B** : Mixte (Domaine pro SSO + Perso OTP email).
- **Type C** : Boîtes personnelles uniquement (OTP email).

## Workflows Critiques
1. **Création** : Opérateur crée Company -> Configure Auth -> Invite Admin Company (lien 7j).
2. **Login** : Point d'entrée unique `payzen.com/login`. Routage auto par Entra selon l'email.
3. **Session** : Timeout d'inactivité de 30 minutes. Refresh token silencieux.
4. **Sécurité** : Lien "Gérer ma sécurité" redirige vers l'IdP source (ex: myaccount.microsoft.com).

## Modèle de Responsabilité
- **Backoffice** : Configure l'auth et crée les admins initiaux.
- **Admin Company** : Invite ses utilisateurs et gère les rôles internes.