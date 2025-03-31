<script>
import Layout from "@/layouts/main";
import PageHeader from "@/components/page-header";
import Multiselect from "vue-multiselect";
import DatePicker from "vue2-datepicker";


export default {
  components: { Layout, PageHeader },
  data() {
    return {
      title: "Lịch sử gọi API",
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

    async exportExcel() {
      try {
        const response = await this.$store.dispatch("monitorStore/exportExcel");

        // Đảm bảo response là dạng Blob
        const blob = new Blob([response], {
          type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        });

        // Tạo link tải file
        const link = document.createElement("a");
        link.href = window.URL.createObjectURL(blob);
        link.download = "LichSuGoiAPI.xlsx";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        this.$bvToast.toast("Xuất Excel thành công", { variant: "success" });
      } catch (error) {
        console.error("Lỗi khi xuất Excel:", error);
        this.$bvToast.toast("Lỗi khi xuất Excel", { variant: "danger" });
      }
    },

    formatDate(datetime) {
      if (!datetime) return "N/A";
      const date = new Date(datetime);
      return `${date.getUTCDate().toString().padStart(2, "0")}/${(date.getUTCMonth() + 1).toString().padStart(2, "0")}/${date.getUTCFullYear()} ${date.getUTCHours().toString().padStart(2, "0")}:${date.getUTCMinutes().toString().padStart(2, "0")}:${date.getUTCSeconds().toString().padStart(2, "0")}`;
    },
  },

  mounted() {
    this.getCallHistory();
    this.getDropdownData();
  }
};
</script>
<template>
  <Layout>
    <div class="container-fluid">
      <div class="row">
        <PageHeader :title="title" :items="items" />
        <div class="col-12">
          <b-card>
            <div class="d-flex justify-content-between align-items-center mb-3">
              <b-button class="cs-btn-primary" variant="success" @click="exportExcel">
                <i class="fas fa-file-excel"></i> Xuất Excel
              </b-button>
              <b-form-input v-model="searchQuery" placeholder="Tìm kiếm theo URL" class="w-50"></b-form-input>
            </div>

            <div class="table-responsive">
              <b-table :items="filteredItems" :fields="fields" striped bordered responsive="sm">
                <template #cell(time)="row">{{ formatDate(row.item.time) }}</template>
                <template #cell(phuongThuc)="row">
                  {{ row.item.phuongThuc?.name || "Không xác định" }}
                </template>
                <template #cell(name)="row">
                  <span :class="{ 'text-danger': row.item.trangThai?.name !== 'Thành công' }">
                    {{ row.item.name }}
                  </span>
                </template>
                <template #cell(trangThai)="row">
                  <span :class="{ 'text-danger': row.item.trangThai?.name !== 'Thành công' }">
                    {{ row.item.trangThai?.name || "N/A" }}
                  </span>
                </template>
                <template #cell(actions)="row">
                  <b-button size="sm" variant="danger">Xóa</b-button>
                </template>
              </b-table>
            </div>
          </b-card>
        </div>
      </div>
    </div>
  </Layout>
</template>

<style>
.table th {
  text-align: center;
}
</style>
