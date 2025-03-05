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

      // Dữ liệu nhập từ form
      apiRequest: {
        method: "",
        name: "",
        url: "",
        body: '{ "start": 0, "limit": 5, "serviceId": "string" }' // Mặc định nhập JSON
      },

      listPhuongThuc: [],
      selectedPhuongThuc: null,
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
    // Lấy lịch sử API
    async getCallHistory() {
      try {
        let response = await this.$store.dispatch("monitorStore/getallcallHistory");
        console.log(" Lịch sử API:", response.data);
        this.itemsData = response.data;
      } catch (error) {
        console.error(" Lỗi lấy lịch sử API:", error);
      }
    },

    // Gọi API
    async create() {
      try {
        console.log(" Gọi API...");

        // Kiểm tra chọn phương thức chưa
        if (!this.selectedPhuongThuc || !this.selectedPhuongThuc.name) {
          this.$bvToast.toast("Vui lòng chọn phương thức API!", { variant: "warning" });
          return;
        }

        let bodyParams = "{}"; // Mặc định JSON rỗng
        if (this.apiRequest.body.trim()) {
          try {
            console.log(" Kiểm tra JSON body:", this.apiRequest.body);
            const parsedBody = JSON.parse(this.apiRequest.body); // Kiểm tra JSON hợp lệ
            bodyParams = JSON.stringify(parsedBody, null, 2); // Format JSON lại đẹp hơn
          } catch (e) {
            console.error(" JSON body không hợp lệ!", e);
            this.$bvToast.toast("Lỗi: JSON body không hợp lệ! Vui lòng kiểm tra lại.", { variant: "danger" });
            return;
          }
        }

        const requestData = {
          name: this.apiRequest.name,
          url: this.apiRequest.url,
          phuongThuc: { name: this.selectedPhuongThuc.name.toUpperCase() },
          bodyParams: bodyParams
        };

        console.log(" Dữ liệu gửi lên:", requestData);

        await this.$store.dispatch("monitorStore/create", requestData);
        this.$bvToast.toast("Gọi API thành công", { variant: "success" });

        this.getCallHistory(); // Load lại danh sách API
      } catch (error) {
        console.error(" Lỗi khi gọi API:", error);
        this.$bvToast.toast("Lỗi khi gọi API", { variant: "danger" });
      }
    },

    // Lấy danh sách phương thức API
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

        <!--  Form gọi API -->
        <b-card title="Gọi API">
          <b-form @submit.prevent="create">
            <b-form-group label="Phương thức API">
              <multiselect
                v-model="selectedPhuongThuc"
                :options="listPhuongThuc"
                label="name"
                track-by="name"
                placeholder="Chọn phương thức API"
              />
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

        <!--  Lịch sử gọi API -->
        <b-card title="Lịch sử gọi API">
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
      </div>
    </div>
  </Layout>
</template>

<style>
.table th {
  text-align: center;
}
</style>
