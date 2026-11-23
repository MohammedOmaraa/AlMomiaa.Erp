export interface AccessTokenStore {
  getAccessToken: () => string | null;
  setAccessToken: (token: string | null) => void;
  clearAccessToken: () => void;
}

/**
 * Strictly in-memory access token storage.
 * Never persists tokens to localStorage, sessionStorage, IndexedDB, cookies, or logs.
 */
export function createAccessTokenStore(): AccessTokenStore {
  let inMemoryAccessToken: string | null = null;

  return {
    getAccessToken: () => inMemoryAccessToken,
    setAccessToken: (token: string | null) => {
      inMemoryAccessToken = token && token.trim().length > 0 ? token : null;
    },
    clearAccessToken: () => {
      inMemoryAccessToken = null;
    },
  };
}
