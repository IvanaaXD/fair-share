import { useEffect, useState } from 'react';
import { categoriesApi } from '../api/categories';
import type { Category } from '../api/types';

// The list is the same for every user and changes rarely, so it is loaded once per visit and
// shared by every screen (filters, expense form, budgets). A failed load is retried next time.
let cached: Promise<Category[]> | null = null;

function loadCategories(): Promise<Category[]> {
  if (!cached) {
    cached = categoriesApi.getAll().catch((error: unknown) => {
      cached = null;
      throw error;
    });
  }
  return cached;
}

/** Call after a category is created, renamed or deleted. */
export function invalidateCategories() {
  cached = null;
}

export function useCategories() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [error, setError] = useState<unknown>(null);

  useEffect(() => {
    let active = true;
    loadCategories().then(
      (result) => {
        if (active) {
          setCategories(result);
          setError(null);
        }
      },
      (failure: unknown) => {
        if (active) {
          setError(failure);
        }
      },
    );
    return () => {
      active = false;
    };
  }, []);

  return { categories, error };
}
