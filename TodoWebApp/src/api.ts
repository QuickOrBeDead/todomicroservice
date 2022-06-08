import { BaseAPI as TaskManagmentApi } from "./metadata/task-management-api/base"

export function createTaskManagementApi<T extends TaskManagmentApi>(ctor: { new (): T }) : T {
    const api = new ctor()
    api["basePath"] = "http://localhost:8081"
    return api
}