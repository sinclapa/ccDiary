import { PublicClientApplication, type AccountInfo, type RedirectRequest} from '@azure/msal-browser'
import { reactive } from 'vue';

export const msalConfig = {
    auth: {
        clientId: import.meta.env.VITE_CLIENTID,
        authority: "https://login.microsoftonline.com/" + import.meta.env.VITE_TENANTID,
        redirectUri: window.location.origin,
        postLogoutRedirectUri: window.location.origin
    },
    cache: {
        cacheLocation: 'sessionStorage',
        storeAuthStateInCookie: false
    }
}

export const graphScopes: RedirectRequest = {
    scopes: ['openid', 'profile']
};

export const state = reactive({
    isAuthenticated: false,
    user: null as AccountInfo | null
});

export const msalInstance = new PublicClientApplication(msalConfig);
