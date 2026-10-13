import { useEffect, useState } from 'react';
import { fetchProtectedImage } from '../api/expenses';

type State = { url: string | null; status: 'idle' | 'loading' | 'ready' | 'error' };

/**
 * Shows an image that needs the access token (a receipt). The image is downloaded with the
 * token and turned into a temporary object URL, which is released when it is no longer shown.
 */
export function useProtectedImage(source: string | null): State {
  const [state, setState] = useState<State & { source: string | null }>({ url: null, status: 'idle', source: null });

  useEffect(() => {
    if (!source) {
      return;
    }
    const controller = new AbortController();
    let objectUrl: string | null = null;

    fetchProtectedImage(source, controller.signal).then(
      (blob) => {
        objectUrl = URL.createObjectURL(blob);
        setState({ url: objectUrl, status: 'ready', source });
      },
      (error: unknown) => {
        if (!controller.signal.aborted) {
          console.warn('Image could not be loaded', error);
          setState({ url: null, status: 'error', source });
        }
      },
    );

    return () => {
      controller.abort();
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [source]);

  if (!source) {
    return { url: null, status: 'idle' };
  }
  // While a new source loads, the previous image is not shown any more.
  if (state.source !== source) {
    return { url: null, status: 'loading' };
  }
  return { url: state.url, status: state.status };
}
