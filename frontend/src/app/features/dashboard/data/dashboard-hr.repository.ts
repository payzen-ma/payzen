import { Observable } from 'rxjs';
import { DashboardHrPayload, DashboardHrQuery } from '../state/dashboard-hr.models';
import { DashboardHrRawData } from './dashboard-hr-raw.models';

export abstract class DashboardHrRepository {
  abstract getDashboardData(query: DashboardHrQuery): Observable<DashboardHrPayload>;
  abstract getDashboardRawData(query: DashboardHrQuery): Observable<DashboardHrRawData>;
}
