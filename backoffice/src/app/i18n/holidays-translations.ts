export interface HolidayTranslations {
  en: string;
  fr: string;
  ar: string;
}

// Keyed by a normalized English name (lowercase, punctuation removed)
export const HOLIDAY_TRANSLATIONS: Record<string, HolidayTranslations> = {
  "new years day": { en: "New Year's Day", fr: "Jour de l'An", ar: "رأس السنة الميلادية" },
  "anniversary of the independence manifesto": { en: "Anniversary of the Independence Manifesto", fr: "Anniversaire du Manifeste de l'Indépendance", ar: "ذكرى بيان الاستقلال" },
  "amazigh new year": { en: "Amazigh New Year", fr: "Nouvel An amazigh", ar: "رأس السنة الأمازيغية" },
  "eid al-fitr": { en: "Eid al-Fitr", fr: "Aïd al-Fitr", ar: "عيد الفطر" },
  "eid al-fitr holiday": { en: "Eid al-Fitr Holiday", fr: "Férié Aïd al-Fitr", ar: "عطلة عيد الفطر" },
  "labour day/may day": { en: "Labour Day/May Day", fr: "Fête du Travail / 1er mai", ar: "عيد العمال" },
  "eid al-adha": { en: "Eid al-Adha", fr: "Aïd al-Adha", ar: "عيد الأضحى" },
  "eid al-adha holiday": { en: "Eid al-Adha Holiday", fr: "Férié Aïd al-Adha", ar: "عطلة عيد الأضحى" },
  "hijra new year": { en: "Hijra New Year", fr: "Nouvel An Hijri", ar: "رأس السنة الهجرية" },
  "feast of the throne": { en: "Feast of the Throne", fr: "Fête du Trône", ar: "عيد العرش" },
  "anniversary of the recovery oued ed-dahab": { en: "Anniversary of the Recovery Oued Ed-Dahab", fr: "Anniversaire de la récupération d'Oued Ed-Dahab", ar: "ذكرى استرجاع وادي الذهب" },
  "anniversary of the revolution of the king and the people": { en: "Anniversary of the Revolution of the King and the People", fr: "Anniversaire de la Révolution du Roi et du Peuple", ar: "ذكرى ثورة الملك والشعب" },
  "youth day": { en: "Youth Day", fr: "Journée de la Jeunesse", ar: "عيد الشباب" },
  "the prophet muhammad's birthday": { en: "The Prophet Muhammad's Birthday", fr: "Anniversaire du Prophète Muhammad", ar: "مولد النبي محمد" },
  "the prophet muhammad's birthday holiday": { en: "The Prophet Muhammad's Birthday Holiday", fr: "Férié - Anniversaire du Prophète", ar: "عطلة مولد النبي" },
  "anniversary of the green march": { en: "Anniversary of the Green March", fr: "Anniversaire de la Marche Verte", ar: "ذكرى المسيرة الخضراء" },
  "independence day": { en: "Independence Day", fr: "Fête de l'Indépendance", ar: "عيد الاستقلال" }
};

export function normalizeHolidayName(name: string | undefined): string {
  if (!name) return '';
  return name
    .toLowerCase()
    .replace(/[^a-z0-9\u0600-\u06FF\s]/gi, '') // remove punctuation but allow arabic unicode
    .replace(/\s+/g, ' ')
    .trim();
}
