/* tslint:disable */
/* eslint-disable */
/**
 * TaskManagementApi
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * The version of the OpenAPI document: 1.0
 * 
 *
 * NOTE: This class is auto generated by OpenAPI Generator (https://openapi-generator.tech).
 * https://openapi-generator.tech
 * Do not edit the class manually.
 */


import { Configuration } from './configuration';
import globalAxios, { AxiosPromise, AxiosInstance, AxiosRequestConfig } from 'axios';
// Some imports not used depending on template conditions
// @ts-ignore
import { DUMMY_BASE_URL, assertParamExists, setApiKeyToObject, setBasicAuthToObject, setBearerAuthToObject, setOAuthToObject, setSearchParams, serializeDataIfNeeded, toPathString, createRequestFunction } from './common';
// @ts-ignore
import { BASE_PATH, COLLECTION_FORMATS, RequestArgs, BaseAPI, RequiredError } from './base';

/**
 * 
 * @export
 * @interface TaskEntity
 */
export interface TaskEntity {
    /**
     * 
     * @type {number}
     * @memberof TaskEntity
     */
    'id'?: number;
    /**
     * 
     * @type {string}
     * @memberof TaskEntity
     */
    'title'?: string | null;
    /**
     * 
     * @type {boolean}
     * @memberof TaskEntity
     */
    'completed'?: boolean;
}

/**
 * TaskApi - axios parameter creator
 * @export
 */
export const TaskApiAxiosParamCreator = function (configuration?: Configuration) {
    return {
        /**
         * 
         * @param {string} [title] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        addTask: async (title?: string, options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/Task`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, DUMMY_BASE_URL);
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }

            const localVarRequestOptions = { method: 'POST', ...baseOptions, ...options};
            const localVarHeaderParameter = {} as any;
            const localVarQueryParameter = {} as any;

            if (title !== undefined) {
                localVarQueryParameter['title'] = title;
            }


    
            setSearchParams(localVarUrlObj, localVarQueryParameter);
            let headersFromBaseOptions = baseOptions && baseOptions.headers ? baseOptions.headers : {};
            localVarRequestOptions.headers = {...localVarHeaderParameter, ...headersFromBaseOptions, ...options.headers};

            return {
                url: toPathString(localVarUrlObj),
                options: localVarRequestOptions,
            };
        },
        /**
         * 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        getTasks: async (options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/Task`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, DUMMY_BASE_URL);
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }

            const localVarRequestOptions = { method: 'GET', ...baseOptions, ...options};
            const localVarHeaderParameter = {} as any;
            const localVarQueryParameter = {} as any;


    
            setSearchParams(localVarUrlObj, localVarQueryParameter);
            let headersFromBaseOptions = baseOptions && baseOptions.headers ? baseOptions.headers : {};
            localVarRequestOptions.headers = {...localVarHeaderParameter, ...headersFromBaseOptions, ...options.headers};

            return {
                url: toPathString(localVarUrlObj),
                options: localVarRequestOptions,
            };
        },
    }
};

/**
 * TaskApi - functional programming interface
 * @export
 */
export const TaskApiFp = function(configuration?: Configuration) {
    const localVarAxiosParamCreator = TaskApiAxiosParamCreator(configuration)
    return {
        /**
         * 
         * @param {string} [title] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async addTask(title?: string, options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => AxiosPromise<void>> {
            const localVarAxiosArgs = await localVarAxiosParamCreator.addTask(title, options);
            return createRequestFunction(localVarAxiosArgs, globalAxios, BASE_PATH, configuration);
        },
        /**
         * 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async getTasks(options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => AxiosPromise<Array<TaskEntity>>> {
            const localVarAxiosArgs = await localVarAxiosParamCreator.getTasks(options);
            return createRequestFunction(localVarAxiosArgs, globalAxios, BASE_PATH, configuration);
        },
    }
};

/**
 * TaskApi - factory interface
 * @export
 */
export const TaskApiFactory = function (configuration?: Configuration, basePath?: string, axios?: AxiosInstance) {
    const localVarFp = TaskApiFp(configuration)
    return {
        /**
         * 
         * @param {string} [title] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        addTask(title?: string, options?: any): AxiosPromise<void> {
            return localVarFp.addTask(title, options).then((request) => request(axios, basePath));
        },
        /**
         * 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        getTasks(options?: any): AxiosPromise<Array<TaskEntity>> {
            return localVarFp.getTasks(options).then((request) => request(axios, basePath));
        },
    };
};

/**
 * TaskApi - object-oriented interface
 * @export
 * @class TaskApi
 * @extends {BaseAPI}
 */
export class TaskApi extends BaseAPI {
    /**
     * 
     * @param {string} [title] 
     * @param {*} [options] Override http request option.
     * @throws {RequiredError}
     * @memberof TaskApi
     */
    public addTask(title?: string, options?: AxiosRequestConfig) {
        return TaskApiFp(this.configuration).addTask(title, options).then((request) => request(this.axios, this.basePath));
    }

    /**
     * 
     * @param {*} [options] Override http request option.
     * @throws {RequiredError}
     * @memberof TaskApi
     */
    public getTasks(options?: AxiosRequestConfig) {
        return TaskApiFp(this.configuration).getTasks(options).then((request) => request(this.axios, this.basePath));
    }
}


