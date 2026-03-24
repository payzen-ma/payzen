export type TagVariant = 'default' | 'primary' | 'success' | 'warning' | 'danger' | 'info';
export type TagSize = 'sm' | 'md' | 'lg';

export interface TagConfig {
  label: string;
  variant?: TagVariant;
  icon?: string; // Optional icon name (e.g., for Phosphor/FontAwesome)
}