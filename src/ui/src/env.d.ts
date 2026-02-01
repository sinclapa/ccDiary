declare global {
  interface Window {
    APP_CONFIG?: {
      VITE_BUILD_NUMBER?: string;
      VITE_API?: string;
      VITE_CLIENT_ID?: string;
      VITE_TENANT_ID?: string;
      VITE_APPLICATION_ID_URI?: string;
      [key: string]: any;
    };
  }
}

export {};
