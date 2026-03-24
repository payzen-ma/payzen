import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { map } from 'rxjs/operators';

const JSON_MEDIA_TYPE = 'application/json';

const isPlainObject = (value: unknown): value is Record<string, unknown> => {
  if (value === null || typeof value !== 'object') {
    return false;
  }
  const proto = Object.getPrototypeOf(value);
  return proto === Object.prototype || proto === null;
};

const toCamelCase = (key: string): string => {
  if (!key) {
    return key;
  }
  const lowerFirst = key.charAt(0).toLowerCase() + key.slice(1);
  return lowerFirst.replace(/[-_\s]+(.)?/g, (_, chr) => (chr ? chr.toUpperCase() : ''));
};

const camelCaseDeep = (value: unknown): unknown => {
  if (Array.isArray(value)) {
    return value.map(item => camelCaseDeep(item));
  }

  if (isPlainObject(value)) {
    return Object.entries(value).reduce<Record<string, unknown>>((acc, [key, val]) => {
      acc[toCamelCase(key)] = camelCaseDeep(val);
      return acc;
    }, {});
  }

  return value;
};

const shouldTransform = (response: HttpResponse<unknown>): boolean => {
  const contentType = response.headers.get('content-type') ?? '';
  return (
    !!response.body &&
    typeof response.body === 'object' &&
    contentType.toLowerCase().includes(JSON_MEDIA_TYPE)
  );
};

export const camelCaseInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    map(event => {
      if (!(event instanceof HttpResponse)) {
        return event;
      }

      if (!shouldTransform(event)) {
        return event;
      }

      const transformedBody = camelCaseDeep(event.body);
      return event.clone({ body: transformedBody });
    })
  );
