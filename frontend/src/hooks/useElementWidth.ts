import { useCallback, useState } from 'react';

/**
 * Width of an element in pixels, updated when it changes (window resize, sidebar opening...).
 * Charts are drawn in real pixels so text and bar widths keep their size at any width.
 * Returns a callback ref: <div ref={ref}>.
 */
export function useElementWidth<T extends HTMLElement>(): [(element: T | null) => void, number] {
  const [width, setWidth] = useState(0);

  const ref = useCallback((element: T | null) => {
    if (!element) return;
    setWidth(element.getBoundingClientRect().width);
    const observer = new ResizeObserver(([entry]) => setWidth(entry.contentRect.width));
    observer.observe(element);
    // React 19 calls the cleanup returned by a callback ref when the element goes away.
    return () => observer.disconnect();
  }, []);

  return [ref, width];
}
