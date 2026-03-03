# PayZen - Guide des Variables CSS Globales

## 📍 Où modifier les couleurs et styles

**Fichier principal:** `src/styles.css`

Toutes les couleurs, espacements et styles de l'application sont définis dans ce fichier via des variables CSS. Modifier une variable ici changera l'apparence dans toute l'application.

## 🎨 Variables de Couleurs Principales

### Couleur Primaire (Bleu PayZen)
```css
--primary-500: #1a73e8;    /* Couleur principale */
--primary-600: #1557b0;    /* Hover state */
--primary-100: #d6ebff;    /* Fond clair */
--primary-700: #0f4187;    /* Couleur foncée */
```

**Utilisation dans le code:**
- Icônes dans les badges: `bg-linear-to-br from-blue-500 to-blue-600`
- Texte: `text-blue-700`
- Badges: `bg-blue-50 text-blue-700`

### Couleurs Sémantiques

#### Succès (Vert)
```css
--success: var(--color-green-500);
--success-hover: var(--color-green-600);
--success-light: var(--color-green-100);
```

#### Avertissement (Orange)
```css
--warning: var(--color-orange-500);
--warning-hover: var(--color-orange-600);
```

#### Danger (Rouge)
```css
--danger: var(--color-red-500);
--danger-hover: var(--color-red-600);
```

#### Info (Bleu)
```css
--info: var(--color-blue-500);
```

## 📐 Variables d'Arrière-plan

```css
--bg-page: #f8fafc;        /* Fond de page */
--bg-element: #ffffff;     /* Fond des cards/éléments */
--bg-hover: #f9fafb;       /* Hover state */
--bg-active: #f3f4f6;      /* Active state */
```

## ✏️ Variables de Texte

```css
--text-primary: #1f2937;    /* Texte principal (titres) */
--text-secondary: #6b7280;  /* Texte secondaire (descriptions) */
--text-muted: #9ca3af;      /* Texte atténué */
--text-inverse: #ffffff;    /* Texte sur fond foncé */
```

## 📏 Variables de Bordures

```css
--border-color-subtle: #e5e7eb;   /* Bordures légères */
--border-color-medium: #d1d5db;   /* Bordures moyennes */
--border-color-strong: #9ca3af;   /* Bordures fortes */
```

## 🔄 Variables de Rayons (Border-radius)

```css
--rads-sm: 4px;      /* Petit rayon */
--rads-md: 6px;      /* Moyen rayon */
--rads-lg: 8px;      /* Grand rayon */
--rads-xl: 12px;     /* Très grand rayon */
--rads-full: 9999px; /* Arrondi complet (cercle/pill) */
```

## 📦 Exemple d'Application dans les Composants

### Holidays Component (`holidays.html`)

```html
<!-- Badge avec icône - utilise les variables via Tailwind -->
<div class="shrink-0 size-12 rounded-xl bg-linear-to-br from-blue-500 to-blue-600">
  <i class="pi pi-calendar-plus text-white text-xl"></i>
</div>

<!-- Card - utilise les variables pour bordure et rayon -->
<section class="bg-white rounded-xl border border-gray-200 shadow-sm">
  <!-- Contenu -->
</section>

<!-- Badge de statistique -->
<span class="bg-blue-50 text-blue-700 rounded-full">
  {{ holidays().length }} holidays
</span>
```

### CSS Personnalisé (`holidays.css`)

```css
.checkboxes-grid {
  background: var(--bg-hover);           /* Utilise la variable d'arrière-plan */
  border: 1px solid var(--border-color-subtle);  /* Utilise la variable de bordure */
  border-radius: var(--rads-lg);        /* Utilise le rayon de 8px */
}

.checkbox-label {
  color: var(--text-primary);           /* Utilise la variable de texte principal */
}
```

## 🎯 Comment Changer une Couleur Globalement

### Exemple: Changer le bleu primaire en violet

1. Ouvrir `src/styles.css`
2. Modifier les variables de la section PRIMARY COLOR SCALE:

```css
:root {
  --primary-50: #f5f3ff;
  --primary-100: #ede9fe;
  --primary-200: #ddd6fe;
  --primary-300: #c4b5fd;
  --primary-400: #a78bfa;
  --primary-500: #8b5cf6;    /* Nouveau violet principal */
  --primary-600: #7c3aed;
  --primary-700: #6d28d9;
  --primary-800: #5b21b6;
  --primary-900: #4c1d95;
}
```

3. Sauvegarder le fichier
4. Tous les éléments bleus de l'application deviennent violets automatiquement! ✨

## 📋 Checklist pour Ajouter un Nouveau Composant

Lorsque vous créez un nouveau composant, suivez ce pattern:

1. **HTML**: Utiliser les classes Tailwind qui utilisent les variables
   - `bg-white`, `border-gray-200`, `rounded-xl`
   - `text-gray-900`, `text-sm`
   - `p-6`, `gap-4`

2. **CSS Personnalisé**: Utiliser les variables CSS pour tout style custom
   ```css
   .mon-element {
     background: var(--bg-element);
     color: var(--text-primary);
     border: 1px solid var(--border-color-subtle);
   }
   ```

3. **Ne PAS** utiliser de valeurs en dur comme:
   ❌ `background: #ffffff;`
   ❌ `color: #1f2937;`
   
   ✅ `background: var(--bg-element);`
   ✅ `color: var(--text-primary);`

## 🚀 Avantages de Cette Approche

1. **Cohérence**: Tous les composants utilisent les mêmes couleurs
2. **Maintenabilité**: Changer une couleur à un seul endroit
3. **Thématisation**: Facile d'ajouter un mode sombre ou d'autres thèmes
4. **Performance**: Les variables CSS sont natives et performantes

## 📚 Ressources

- [Documentation Tailwind CSS](https://tailwindcss.com/docs)
- [CSS Variables (MDN)](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)
- `src/styles.css` - Fichier de variables global
