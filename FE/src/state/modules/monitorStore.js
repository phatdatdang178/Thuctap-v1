import { apiClient } from "@/state/modules/apiClient";

const controller = "MonitorApi";
export const actions = {
  async create({ commit }, params) {
    return apiClient.post(controller + "/create", params);
  },
  async createSchedule({ commit }, params) {
    return apiClient.post(controller + "/create-schedule", params);
  },
  async getpagingParams({ commit }, params) {
    return apiClient.post(controller + "/get-paging-params", params);
  },
  async getallcallHistory({ commit }) {
    return apiClient.get(controller + "/get-all-call-history");
  },
  async getallcallSchedule({ commit }) {
    return apiClient.get(controller + "/get-all-schedule");
  },
  async getAllMethod({ commit }) {
    return apiClient.get(controller + "/get-all-method");
  },
};

