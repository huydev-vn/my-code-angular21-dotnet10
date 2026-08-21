import type { PageResult } from '../../../core/http/page-result.model';
import type {
  OrganizationUnit,
  PermissionDefinition,
  UserGroup,
} from '../models/authorization.models';

export interface PermissionDefinitionDto {
  id: string;
  code: string;
  name: string;
  module: string | null;
  action: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface UserGroupDto {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface OrganizationUnitDto {
  id: string;
  name: string;
  code: string;
  parentId: string | null;
  isActive: boolean;
  createdAt: string;
}

export function mapPermissionDefinition(dto: PermissionDefinitionDto): PermissionDefinition {
  return {
    id: String(dto.id),
    name: dto.code,
    displayName: dto.name,
    module: dto.module,
    action: dto.action,
    isActive: dto.isActive,
  };
}

export function mapUserGroup(dto: UserGroupDto): UserGroup {
  return {
    id: String(dto.id),
    name: dto.name,
    description: dto.description,
    isActive: dto.isActive,
  };
}

export function mapOrganizationUnit(dto: OrganizationUnitDto): OrganizationUnit {
  return {
    id: String(dto.id),
    name: dto.name,
    code: dto.code,
    parentId: dto.parentId == null ? null : String(dto.parentId),
    isActive: dto.isActive,
  };
}

export function mapPageResult<TDto, TModel>(
  page: PageResult<TDto>,
  mapItem: (item: TDto) => TModel,
): PageResult<TModel> {
  return {
    items: page.items.map(mapItem),
    totalCount: page.totalCount,
    page: page.page,
    pageSize: page.pageSize,
  };
}
