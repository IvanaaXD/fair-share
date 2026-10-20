import { useEffect, useState } from 'react';
import { categoriesApi } from '../api/categories';
import type { Category } from '../api/types';
import { notificationTypeName } from '../notifications/notificationTypes';
import { onNotificationCreated } from '../notifications/realtime';

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

  // A proposal of this user was approved: the list gets the new category without a refresh.
  const [version, setVersion] = useState(0);
  useEffect(
    () =>
      onNotificationCreated((notification) => {
        if (notificationTypeName(notification.type) === 'CategoryReviewed') {
          invalidateCategories();
          setVersion((value) => value + 1);
        }
      }),
    [],
  );

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
  }, [version]);

  return { categories, error };
}
