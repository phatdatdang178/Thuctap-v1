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
  async exportExcel({ commit }) {
    return apiClient.get(controller + "/export-excel", {
      responseType: "blob",
    });
  },
  async deleted({ commit }, id) {
    return await apiClient.post(controller + "/deleted" , id);
  },

  async delete({ commit }, id) {
    return await apiClient.post(controller + "/delete" , id);
  },

  async getById({ commit }, id) {
    return apiClient.post(controller + "/get-by-id", id);
  },

  async update({ commit }, id) {
    return apiClient.post(controller + "/update", id);
  },
};
