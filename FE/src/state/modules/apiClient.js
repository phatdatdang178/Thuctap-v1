import axios from "axios";
import Vue from "vue";

const baseURL = process.env.VUE_APP_API_URL; // Ví dụ: "https://localhost:5001/api/Monitor/"

export const httpClient = axios.create({
  baseURL: baseURL,
  headers: {
    "Content-Type": "application/json"
  },
  timeout: 300000 // 5 phút timeout
});

// Ví dụ về cách quản lý token:
class ApiClient {
  getInstance() {
    if (!Vue.prototype.$auth_token) {
      const token = localStorage.getItem("user-token");
      // Nếu token đã được lưu dạng JSON, parse nó; nếu không, giữ nguyên
      Vue.prototype.$auth_token = token ? JSON.parse(token) : null;
    }
    if (Vue.prototype.$auth_token) {
      httpClient.defaults.headers.common["Authorization"] = `Bearer ${Vue.prototype.$auth_token}`;
    }
    return httpClient;
  }

  async get(url, config = null) {
    try {
      // Vì baseURL đã được set, chỉ cần truyền url tương đối (ví dụ: "history")
      const response = await this.getInstance().get(url, config);
      return response.data;
    } catch (e) {
      return {
        success: false,
        code: CLIENT_ERROR_CODE,
        message: e.toString()
      };
    }
  }

  async post(url, data, config = null) {
    try {
      const response = await this.getInstance().post(url, data, config);
      return response.data;
    } catch (e) {
      return {
        success: false,
        code: CLIENT_ERROR_CODE,
        message: e.toString()
      };
    }
  }

  // Tương tự cho các phương thức khác...
}

export const CLIENT_ERROR_CODE = 400;
export const apiClient = new ApiClient();
