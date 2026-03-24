export interface City {
  id: number;
  name: string;
  countryId?: number;
  countryName?: string;
}

export interface Country {
  id: number;
  name: string;
  code?: string;
  phoneCode?: string;
  nameAr?: string;
}

export interface CompanyFormData {
  cities: City[];
  countries: Country[];
}

export interface Company {
  id: number;
  companyName: string;
  isCabinetExpert: boolean;
  email: string;
  phoneNumber: number;
  countryCode?: string;
  cityName: string;
  countryName: string;
  cnssNumber: string;
  createdAt: string;
  companyAddress:string;
  
  status?: 'active' | 'inactive';
  isActive?: boolean;
  licence?: string;
  
  // Legal & Fiscal
  iceNumber?: string;
  ifNumber?: string;
  rcNumber?: string;
  legalForm?: string;
  foundingDate?: string;
  patentNumber?: string;
}

export interface CompanyCreateRequest {
  companyName: string;
  email: string;
  phoneNumber: string;
  countryPhoneCode: string;
  companyAddress: string;
  countryId: number;
  cityId?: number;
  cityName?: string;
  cnssNumber: string;
  isCabinetExpert: boolean;
  
  // Admin account
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  adminDateOfBirth?: string;
  adminPhone?: string;
  adminPassword?: string;
  generatePassword: boolean;
}

export interface CompanyCreateResponse {
  company: {
    id: number;
    companyName: string;
    email: string;
    phoneNumber: string;
    countryPhoneCode: string;
    companyAddress: string;
    cityId: number;
    cityName: string;
    countryId: number;
    countryName: string;
    cnssNumber: string;
    isCabinetExpert: boolean;
    createdAt: string;
  };
  admin: {
    employeeId: number;
    userId: number;
    username: string;
    email: string;
    firstName: string;
    lastName: string;
    phone: string;
    password?: string;
    message?: string;
  };
}
export interface PartialUpdateRequest {
    companyName?: string;
    email?: string;
    phoneNumber?: string;
    address?: string;
    cityName?: string;
    countryName?: string;
    cnssNumber?: string;
    status?: string;
  isActive?: boolean;
    isCabinetExpert?: boolean;
    licence?: string;
    // Legal & Fiscal
    iceNumber?: string;
    identifiantFiscal?: string;
    rcNumber?: string;
    formeJuridique?: string;
    foundingDate?: string;
  }

export interface CompanyUpdateRequest {
  companyName: string;
  email: string;
  phoneNumber: string;
  address: string;
  cityName: string;
  countryName: string;
  cnssNumber: string;
  status?: string;
  isCabinetExpert: boolean;
  licence: string;
  
  // Legal & Fiscal
  iceNumber?: string;
  identifiantFiscal?: string;
  rcNumber?: string;
  formeJuridique?: string;
  foundingDate?: string;
}
