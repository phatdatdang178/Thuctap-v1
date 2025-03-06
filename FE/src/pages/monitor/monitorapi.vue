<script>
import Layout from "@/layouts/main";
import PageHeader from "@/components/page-header";
import Multiselect from "vue-multiselect";

export default {
  components: { Layout, PageHeader, Multiselect },
  data() {
    return {
      title: "Giám sát API",
      items: [{ text: "Monitor", href: "/monitor" }, { text: "Gọi API & Lịch trình", active: true }],
      fields: [
        { key: "name", label: "Tên API", sortable: true },
        { key: "url", label: "URL", sortable: true },
        { key: "time", label: "Thời gian", sortable: true },
        { key: "trangThai", label: "Trạng thái", sortable: true },
        { key: "phuongThuc", label: "Phương thức", sortable: true },
        { key: "code", label: "Mã phản hồi", sortable: true },
        { key: "actions", label: "Thao tác" }
      ],
      itemsData: [],

      apiRequest: {
        method: "",
        name: "",
        url: "",
        body: '{ "start": 0, "limit": 5, "serviceId": "string" }'
      },
      scheduleRequest: {
        method: "",
        name: "",
        url: "",
        body: '{ "start": 0, "limit": 5, "serviceId": "string" }',
        specificTimes: [],
        startTime: "",
        endTime: "",
        callFrequency: null
      },

      listPhuongThuc: [],
      selectedPhuongThuc: null,
      selectedSchedulePhuongThuc: null,
      searchQuery: "",
      perPage: 10,
      currentPage: 1
    };
  },

  computed: {
    filteredItems() {
      return this.itemsData.filter(item => item.url.toLowerCase().includes(this.searchQuery.toLowerCase()));
    }
  },

  methods: {
    async getCallHistory() {
      try {
        let response = await this.$store.dispatch("monitorStore/getallcallHistory");
        console.log(" Lịch sử API:", response.data);
        this.itemsData = response.data;
      } catch (error) {
        console.error(" Lỗi lấy lịch sử API:", error);
      }
    },

    async create() {
      try {
        if (!this.selectedPhuongThuc || !this.selectedPhuongThuc.name) {
          this.$bvToast.toast("Vui lòng chọn phương thức API!", { variant: "warning" });
          return;
        }

        let bodyParams = this.validateJson(this.apiRequest.body);
        if (!bodyParams) return;

        const requestData = {
          name: this.apiRequest.name,
          url: this.apiRequest.url,
          phuongThuc: { name: this.selectedPhuongThuc.name.toUpperCase() },
          bodyParams: bodyParams
        };

        await this.$store.dispatch("monitorStore/create", requestData);
        this.$bvToast.toast("Gọi API thành công", { variant: "success" });

        this.getCallHistory();
      } catch (error) {
        console.error("Lỗi khi gọi API:", error);
        this.$bvToast.toast("Lỗi khi gọi API", { variant: "danger" });
      }
    },

    async schedule() {
      try {
        if (!this.selectedSchedulePhuongThuc || !this.selectedSchedulePhuongThuc.name) {
          this.$bvToast.toast("Vui lòng chọn phương thức API!", { variant: "warning" });
          return;
        }

        let bodyParams = this.validateJson(this.scheduleRequest.body);
        if (!bodyParams) return;

        const scheduleData = {
          monitorApiModel: {
            name: this.scheduleRequest.name,
            url: this.scheduleRequest.url,
            phuongThuc: { name: this.selectedSchedulePhuongThuc.name.toUpperCase() },
            bodyParams: bodyParams, 
          },
          specificTimes: this.scheduleRequest.specificTimes.map(time => time.trim()), // Đảm bảo là array
          startTime: this.scheduleRequest.startTime.trim(),
          endTime: this.scheduleRequest.endTime.trim(),
          callFrequency: Number(this.scheduleRequest.callFrequency) || 0 // Chuyển string thành số
        };

        console.log("Dữ liệu gửi đi:", scheduleData);

        await this.$store.dispatch("monitorStore/createSchedule", scheduleData);
        this.$bvToast.toast("Lên lịch API thành công", { variant: "success" });

        this.getCallHistory();
      } catch (error) {
        console.error("Lỗi khi lên lịch API:", error);
        this.$bvToast.toast("Lỗi khi lên lịch API", { variant: "danger" });
      }
    },

    validateJson(jsonStr) {
      try {
        return JSON.stringify(JSON.parse(jsonStr), null, 2);
      } catch (e) {
        this.$bvToast.toast("Lỗi: JSON body không hợp lệ!", { variant: "danger" });
        return null;
      }
    },

    async getDropdownData() {
      try {
        let resPhuongThuc = await this.$store.dispatch("commonStore/getAll", "DM_PHUONGTHUC");
        this.listPhuongThuc = resPhuongThuc.data || [];
      } catch (error) {
        console.error(" Lỗi khi lấy danh sách phương thức API:", error);
      }
    },

    formatDate(datetime) {
      if (!datetime) return "N/A";
      const date = new Date(datetime);
      return `${date.getUTCDate().toString().padStart(2, "0")}/${(date.getUTCMonth() + 1).toString().padStart(2, "0")}/${date.getUTCFullYear()} ${date.getUTCHours().toString().padStart(2, "0")}:${date.getUTCMinutes().toString().padStart(2, "0")}:${date.getUTCSeconds().toString().padStart(2, "0")}`;
    }
  },

  mounted() {
    this.getCallHistory();
    this.getDropdownData();
  }
};
</script>
<template>
  <Layout>
    <div class="row">
      <div class="col-12">
        <PageHeader :title="title" :items="items" />

        <b-row>
          <!-- Cột trái: Form nhập liệu -->
          <b-col md="6">
            <b-tabs content-class="mt-3">
              <b-tab title="Gọi API" active>
                <b-card title="Gọi API">
                  <b-form @submit.prevent="create">
                    <b-form-group label="Phương thức API">
                      <multiselect v-model="selectedPhuongThuc" :options="listPhuongThuc" label="name" track-by="name"
                        placeholder="Chọn phương thức API" />
                    </b-form-group>
                    <b-form-group label="Tên API">
                      <b-form-input v-model="apiRequest.name" required></b-form-input>
                    </b-form-group>
                    <b-form-group label="URL">
                      <b-form-input v-model="apiRequest.url" required></b-form-input>
                    </b-form-group>
                    <b-form-group label="Body (JSON)">
                      <b-form-textarea v-model="apiRequest.body" rows="5" required></b-form-textarea>
                    </b-form-group>
                    <b-button type="submit" variant="primary">Gọi API</b-button>
                  </b-form>
                </b-card>
              </b-tab>

              <b-tab title="Lên lịch gọi API">
                <b-card title="Lên lịch API">
                  <b-form @submit.prevent="schedule">
                    <b-form-group label="Phương thức API">
                      <multiselect v-model="selectedSchedulePhuongThuc" :options="listPhuongThuc" label="name" track-by="name"
                        placeholder="Chọn phương thức API" />
                    </b-form-group>
                    <b-form-group label="Tên API">
                      <b-form-input v-model="scheduleRequest.name" required />
                    </b-form-group>
                    <b-form-group label="URL">
                      <b-form-input v-model="scheduleRequest.url" required />
                    </b-form-group>
                    <b-form-group label="Body (JSON)">
                      <b-form-textarea v-model="scheduleRequest.body" rows="5" required></b-form-textarea>
                    </b-form-group>
                    <b-form-group label="Giờ gọi (các mốc thời gian)">
                      <b-form-tags v-model="scheduleRequest.specificTimes" separator=" " />
                    </b-form-group>
                    <b-form-group label="Thời điểm bắt đầu">
                      <b-form-input v-model="scheduleRequest.startTime" required />
                    </b-form-group>
                    <b-form-group label="Thời điểm kết thúc">
                      <b-form-input v-model="scheduleRequest.endTime" required />
                    </b-form-group>
                    <b-form-group label="Số lần gọi">
                      <b-form-input v-model="scheduleRequest.callFrequency" required />
                    </b-form-group>
                    <b-button type="submit" variant="success">Lên lịch API</b-button>
                  </b-form>
                </b-card>
              </b-tab>
            </b-tabs>
          </b-col>

          <!-- Cột phải: Bảng dữ liệu -->
          <b-col md="6">
            <b-tabs content-class="mt-3">
              <b-tab title="Lịch sử gọi API" active>
                <b-card>
                  <b-table :items="filteredItems" :fields="fields" striped bordered responsive>
                    <template #cell(time)="row">{{ formatDate(row.item.time) }}</template>
                    <template #cell(phuongThuc)="row">
                      {{ row.item.phuongThuc?.name || "Không xác định" }}
                    </template>
                    <template #cell(trangThai)="row">{{ row.item.trangThai?.name || "N/A" }}</template>
                    <template #cell(actions)="row">
                      <b-button size="sm" variant="danger">Xóa</b-button>
                    </template>
                  </b-table>
                </b-card>
              </b-tab>

              <b-tab title="Lịch gọi API">
                <b-card>
                  <b-table :items="scheduleItems" :fields="scheduleFields" striped bordered responsive>
                    <template #cell(startTime)="row">{{ row.item.startTime }}</template>
                    <template #cell(endTime)="row">{{ row.item.endTime }}</template>
                    <template #cell(callFrequency)="row">{{ row.item.callFrequency }}</template>
                    <template #cell(actions)="row">
                      <b-button size="sm" variant="danger">Xóa</b-button>
                    </template>
                  </b-table>
                </b-card>
              </b-tab>
            </b-tabs>
          </b-col>
        </b-row>

      </div>
    </div>
  </Layout>
</template>
<style>
.table th {
  text-align: center;
}
</style>
