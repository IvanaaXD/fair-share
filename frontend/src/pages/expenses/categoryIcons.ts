import {
  Bike,
  BookOpen,
  Bus,
  Car,
  Coffee,
  Dumbbell,
  Footprints,
  Gift,
  GraduationCap,
  HeartPulse,
  House,
  Lightbulb,
  type LucideIcon,
  PawPrint,
  Plane,
  Popcorn,
  Shirt,
  ShoppingBasket,
  ShoppingBag,
  Smartphone,
  Tag,
  UtensilsCrossed,
  Wifi,
} from 'lucide-react';
import { toLatin } from '../../i18n/transliterate';

/**
 * Categories have no icon on the backend, so the icon is guessed from the name. Names are
 * compared in Latin and lower case, so "Храна", "Hrana" and "Food" all get the same icon.
 * Words are matched by their beginning ("namirnic" covers namirnice, namirnica...).
 */
const RULES: [LucideIcon, string[]][] = [
  [ShoppingBasket, ['namirnic', 'groceries', 'market', 'pijac', 'supermarket']],
  [Coffee, ['kaf', 'coffee', 'cafe', 'kafić', 'pić', 'drinks']],
  [UtensilsCrossed, ['hran', 'food', 'restoran', 'restaurant', 'ručak', 'večer', 'dostav', 'lunch', 'dinner']],
  [Bus, ['prevoz', 'transport', 'autobus', 'bus', 'taksi', 'taxi', 'javni']],
  [Car, ['auto', 'gorivo', 'benzin', 'fuel', 'car', 'parking', 'registracij']],
  [House, ['stan', 'kiri', 'rent', 'smješt', 'smeštaj', 'housing', 'dom', 'home']],
  [Lightbulb, ['režij', 'rezij', 'račun', 'struj', 'voda', 'grijanj', 'utilit', 'bills', 'electric']],
  [Wifi, ['internet', 'telefon', 'phone', 'mobil', 'pretplat', 'subscript']],
  [Popcorn, ['zabav', 'entertain', 'film', 'bioskop', 'kino', 'izlaz', 'izlas', 'fun', 'koncert']],
  [HeartPulse, ['zdrav', 'health', 'apotek', 'ljekov', 'lijek', 'lek', 'medic', 'doktor', 'ljekar', 'pharmac']],
  [Dumbbell, ['sport', 'teretan', 'gym', 'fitnes', 'fitness', 'trening']],
  [Shirt, ['odjeć', 'odjec', 'odeć', 'garderob', 'cloth', 'obuć', 'shoes']],
  [ShoppingBag, ['kupovin', 'shopping', 'šoping', 'soping']],
  [GraduationCap, ['obrazov', 'škol', 'skol', 'fakultet', 'studij', 'educat', 'school', 'kurs', 'course']],
  [BookOpen, ['knjig', 'book', 'literatur']],
  [Plane, ['putov', 'travel', 'odmor', 'ljetovanj', 'vacation', 'let ', 'avion']],
  [Gift, ['poklon', 'gift', 'rođendan', 'rodjendan']],
  [PawPrint, ['ljubim', 'kućni ljub', 'pet', 'veterin']],
  [Smartphone, ['tehnolog', 'elektron', 'tech', 'gadget', 'uređaj']],
];

/** Icons that the Icon column of a category may name (the ones used by the seeded categories). */
const BY_KEY: Record<string, LucideIcon> = {
  'shopping-basket': ShoppingBasket,
  utensils: UtensilsCrossed,
  coffee: Coffee,
  bus: Bus,
  car: Car,
  house: House,
  lightbulb: Lightbulb,
  wifi: Wifi,
  smartphone: Smartphone,
  'heart-pulse': HeartPulse,
  shirt: Shirt,
  footprints: Footprints,
  popcorn: Popcorn,
  dumbbell: Dumbbell,
  bike: Bike,
  'graduation-cap': GraduationCap,
  book: BookOpen,
  plane: Plane,
  gift: Gift,
  'paw-print': PawPrint,
  'shopping-bag': ShoppingBag,
  tag: Tag,
};

/** The icons an administrator or a user can pick for a category, in the order of the picker. */
export const CATEGORY_ICON_KEYS = Object.keys(BY_KEY);

/** The icon picked for a category, or a guess from its name while none is picked. */
export function iconForKey(key: string | null | undefined, name = ''): LucideIcon {
  return categoryIcon(name, key);
}

/** The icon saved with the category if it is a known one, otherwise a guess from the name. */
export function categoryIcon(name: string, iconKey?: string | null): LucideIcon {
  const saved = iconKey ? BY_KEY[iconKey.trim().toLowerCase()] : undefined;
  if (saved) {
    return saved;
  }
  const words = toLatin(name).toLowerCase().split(/[\s,./&-]+/);
  for (const [icon, prefixes] of RULES) {
    if (prefixes.some((prefix) => words.some((word) => word.startsWith(prefix.trim())))) {
      return icon;
    }
  }
  return Tag;
}
