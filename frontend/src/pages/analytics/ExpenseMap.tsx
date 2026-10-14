import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { useTranslation } from 'react-i18next';
import type { ExpenseLocation } from '../../api/types';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import styles from './Analytics.module.css';

interface ExpenseMapProps {
  locations: ExpenseLocation[];
}

const TILES = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
const ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';
/** Banja Luka - shown only until the first expense with a location exists. */
const FALLBACK_CENTER: L.LatLngExpression = [44.7722, 17.191];

/**
 * Where the money was spent: one dot per expense, details in a popup. Leaflet with the free
 * OpenStreetMap tiles - no API key. Loaded only on the analytics page (it is a large library).
 */
export default function ExpenseMap({ locations }: ExpenseMapProps) {
  const { t } = useTranslation();
  const format = useFormatters();
  const localize = useLocalizedText();
  const containerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<L.Map | null>(null);
  const layerRef = useRef<L.LayerGroup | null>(null);

  // The map itself is created once; the dots are replaced when the period changes.
  useEffect(() => {
    if (!containerRef.current) return;
    const map = L.map(containerRef.current, { scrollWheelZoom: false, center: FALLBACK_CENTER, zoom: 12 });
    L.tileLayer(TILES, { maxZoom: 19, attribution: ATTRIBUTION }).addTo(map);
    map.attributionControl.setPrefix('<a href="https://leafletjs.com">Leaflet</a>');
    mapRef.current = map;
    layerRef.current = L.layerGroup().addTo(map);
    return () => {
      map.remove();
      mapRef.current = null;
      layerRef.current = null;
    };
  }, []);

  useEffect(() => {
    const map = mapRef.current;
    const layer = layerRef.current;
    if (!map || !layer) return;
    layer.clearLayers();

    for (const location of locations) {
      const marker = L.circleMarker([location.latitude, location.longitude], {
        radius: 7,
        weight: 2,
        fillOpacity: 1,
        className: styles.marker,
      });
      marker.bindPopup(() => popupContent(location));
      layer.addLayer(marker);
    }

    if (locations.length === 1) {
      map.setView([locations[0].latitude, locations[0].longitude], 15);
    } else if (locations.length > 1) {
      const bounds = L.latLngBounds(locations.map((location) => [location.latitude, location.longitude] as L.LatLngTuple));
      map.fitBounds(bounds, { padding: [32, 32], maxZoom: 15 });
    }

    // Text from the server is put in with textContent, never as HTML.
    function popupContent(location: ExpenseLocation) {
      const box = document.createElement('div');
      box.className = styles.popup;
      const amount = document.createElement('strong');
      amount.className = 'money';
      amount.textContent = format.money(location.amount, location.currency);
      const title = document.createElement('span');
      title.textContent = localize(location.description || location.categoryName);
      const meta = document.createElement('span');
      meta.className = styles.popupMeta;
      meta.textContent = `${localize(location.categoryName)} · ${format.longDate(location.date)}`;
      box.append(amount, title, meta);
      return box;
    }
  }, [locations, format, localize]);

  return <div ref={containerRef} className={styles.map} role="region" aria-label={t('analytics.mapLabel')} />;
}
