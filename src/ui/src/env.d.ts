declare global {
  interface Window {
    APP_CONFIG?: {
      VITE_API?: string;
      VITE_CLIENT_ID?: string;
      VITE_TENANT_ID?: string;
      VITE_APPLICATION_ID_URI?: string;
      VITE_FARO_URL?: string;
      VITE_ENVIRONMENT?: string;
      [key: string]: any;
    };
  }
}

export {};
