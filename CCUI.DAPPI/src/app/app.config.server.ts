import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering } from '@angular/platform-server';
import { appConfig } from './app.config';

/**
 * Server-side rendering configuration for Angular 20+.
 * Note: provideServerRouting was deprecated in Angular 20 and removed.
 * Server routes configuration is now handled differently in Angular 20+.
 */
const serverConfig: ApplicationConfig = {
  providers: [provideServerRendering()],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
