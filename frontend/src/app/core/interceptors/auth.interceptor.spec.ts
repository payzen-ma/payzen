import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';

describe('AuthInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let contextServiceSpy: jasmine.SpyObj<CompanyContextService>;

  beforeEach(() => {
    const authSpy = jasmine.createSpyObj('AuthService', ['getToken']);
    const contextSpy = jasmine.createSpyObj('CompanyContextService', ['companyId']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authSpy },
        { provide: CompanyContextService, useValue: contextSpy }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
    authServiceSpy = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    contextServiceSpy = TestBed.inject(CompanyContextService) as jasmine.SpyObj<CompanyContextService>;
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should add Authorization header when token is present', () => {
    authServiceSpy.getToken.and.returnValue('mock-token');
    contextServiceSpy.companyId.and.returnValue(null);

    httpClient.get('/api/data').subscribe();

    const req = httpMock.expectOne('/api/data');
    expect(req.request.headers.has('Authorization')).toBeTrue();
    expect(req.request.headers.get('Authorization')).toBe('Bearer mock-token');
    expect(req.request.headers.has('X-Company-Id')).toBeFalse();
  });

  it('should add X-Company-Id header when context is selected', () => {
    authServiceSpy.getToken.and.returnValue('mock-token');
    contextServiceSpy.companyId.and.returnValue('123');

    httpClient.get('/api/data').subscribe();

    const req = httpMock.expectOne('/api/data');
    expect(req.request.headers.has('Authorization')).toBeTrue();
    expect(req.request.headers.get('X-Company-Id')).toBe('123');
  });

  it('should NOT add headers for auth endpoints', () => {
    authServiceSpy.getToken.and.returnValue('mock-token');
    contextServiceSpy.companyId.and.returnValue('123');

    httpClient.post('/api/auth/login', {}).subscribe();

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    expect(req.request.headers.has('X-Company-Id')).toBeFalse();
  });

  it('should NOT add X-Company-Id if no company context is selected', () => {
    authServiceSpy.getToken.and.returnValue('mock-token');
    contextServiceSpy.companyId.and.returnValue(null);

    httpClient.get('/api/data').subscribe();

    const req = httpMock.expectOne('/api/data');
    expect(req.request.headers.has('X-Company-Id')).toBeFalse();
  });
});
