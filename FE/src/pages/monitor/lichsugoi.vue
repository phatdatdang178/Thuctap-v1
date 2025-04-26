<script>
import Layout from "@/layouts/main";
import PageHeader from "@/components/page-header";
import {monitorModel} from "@/models/monitorModel"

export default {
  components: { Layout, PageHeader },
  data() {
    return {
      title: "Lịch sử gọi API",
      items: [
        { text: "Monitor", href: "/monitor" }, 
        { text: "Gọi API & Lịch trình", active: true }
      ],
      fields: [
        {
          key: "thongTin",
          label: "Thông tin api",
          sortable: false,
          thStyle: "text-align:center",
          thClass: 'hidden-sortable'
        },
        {
          key: "trangThai", 
          label: "Trạng thái", 
          sortable: true, 
          thStyle: "text-align:center",
        },
        {
          key: "phuongThuc", 
          label: "Phương thức", 
          sortable: true, 
          thStyle: "text-align:center",
        },
        { 
          key: "actions", 
          label: "Thao tác", 
          thClass: 'hidden-sortable' 
        }
      ],
      itemsData: [],
      searchQuery: "",
      perPage: 10,
      currentPage: 1,
      model: monitorModel.baseJson(),
      showDeleteModal: false,
      showModal: false,
      isLoading: false,
      submitted: false,
      listPhuongThuc: []
    };
  },

  computed: {
    filteredItems() {
      return this.itemsData.filter(item => 
        item.url.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        item.name.toLowerCase().includes(this.searchQuery.toLowerCase())
      );
    }
  },

  methods: {
    async getCallHistory() {
      this.isLoading = true;
      try {
        let response = await this.$store.dispatch("monitorStore/getallcallHistory");
        this.itemsData = response.data || [];
      } catch (error) {
        console.error("Lỗi lấy lịch sử API:", error);
        this.$bvToast.toast("Lỗi khi tải lịch sử API", {
          variant: "danger",
          title: "Lỗi"
        });
      } finally {
        this.isLoading = false;
      }
    },

    async handleDelete() {
      if (!this.model._id) return;
      
      this.isLoading = true;
      try {
        const res = await this.$store.dispatch("monitorStore/deleted", { 
          _id: this.model._id 
        });
        
        if (res.code === 0) {
          this.$bvToast.toast("Xóa thành công", {
            variant: "success",
            title: "Thành công"
          });
          await this.getCallHistory();
        } else {
          this.$bvToast.toast(res.message || "Lỗi khi xóa", {
            variant: "danger",
            title: "Lỗi"
          });
        }
      } catch (error) {
        console.error("Lỗi khi xóa:", error);
        this.$bvToast.toast("Lỗi hệ thống khi xóa", {
          variant: "danger",
          title: "Lỗi"
        });
      } finally {
        this.isLoading = false;
        this.showDeleteModal = false;
      }
    },

    confirmDelete(item) {
      this.model = { ...item };
      this.$bvModal.msgBoxConfirm('Bạn có chắc chắn muốn xóa bản ghi này?', {
        title: 'Xác nhận xóa',
        size: 'sm',
        buttonSize: 'sm',
        okVariant: 'danger',
        okTitle: 'Xóa',
        cancelTitle: 'Hủy',
        footerClass: 'p-2',
        hideHeaderClose: false,
        centered: true
      }).then(value => {
        if (value) {
          this.handleDelete();
        }
      });
    },

    async handleUpdate(item) {
      try {
        this.isLoading = true;
        const res = await this.$store.dispatch("monitorStore/getById", { 
          _id: item._id 
        });
        
        if (res.code === 0) {
          this.model = { ...res.data };
          this.showModal = true;
        } else {
          this.$bvToast.toast(res.message || "Lỗi khi lấy thông tin", {
            variant: "danger",
            title: "Lỗi"
          });
        }
      } catch (error) {
        console.error("Lỗi khi lấy thông tin:", error);
        this.$bvToast.toast("Lỗi hệ thống", {
          variant: "danger",
          title: "Lỗi"
        });
      } finally {
        this.isLoading = false;
      }
    },

    async exportExcel() {
      try {
        this.isLoading = true;
        const response = await this.$store.dispatch("monitorStore/exportExcel");
        
        const blob = new Blob([response], {
          type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        });

        const link = document.createElement("a");
        link.href = window.URL.createObjectURL(blob);
        link.download = "LichSuGoiAPI.xlsx";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        this.$bvToast.toast("Xuất Excel thành công", {
          variant: "success",
          title: "Thành công"
        });
      } catch (error) {
        console.error("Lỗi khi xuất Excel:", error);
        this.$bvToast.toast("Lỗi khi xuất Excel", {
          variant: "danger",
          title: "Lỗi"
        });
      } finally {
        this.isLoading = false;
      }
    },

    formatDate(datetime) {
      if (!datetime) return "N/A";
      const date = new Date(datetime);
      return `${date.getDate().toString().padStart(2, "0")}/${(date.getMonth() + 1).toString().padStart(2, "0")}/${date.getFullYear()} ${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}:${date.getSeconds().toString().padStart(2, "0")}`;
    },

    async getDropdownData() {
      try {
        const res = await this.$store.dispatch("monitorStore/getMethods");
        if (res.code === 0) {
          this.listPhuongThuc = res.data || [];
        }
      } catch (error) {
        console.error("Lỗi khi lấy danh sách phương thức:", error);
      }
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
    <div class="container-fluid">
      <div class="row">
        <PageHeader :title="title" :items="items" />
        <div class="col-12">
          <b-card>
            <div class="d-flex justify-content-between align-items-center mb-3">
              <b-form-input 
                v-model="searchQuery" 
                placeholder="Tìm kiếm theo URL hoặc tên" 
                class="w-50"
              ></b-form-input>
              <b-button 
                class="cs-btn-primary" 
                variant="success" 
                @click="exportExcel"
                :disabled="isLoading"
              >
                <i class="fas fa-file-excel"></i> 
                {{ isLoading ? 'Đang xử lý...' : 'Xuất Excel' }}
              </b-button>
            </div>

            <div class="table-responsive">
              <b-table 
                class="datatables" 
                :items="filteredItems" 
                :fields="fields" 
                striped 
                bordered 
                responsive="sm"
                :busy="isLoading"
              >
                <template #table-busy>
                  <div class="text-center text-primary my-2">
                    <b-spinner class="align-middle"></b-spinner>
                    <strong>Đang tải...</strong>
                  </div>
                </template>

                <template #cell(thongTin)="row">
                  <div class="combined-info">
                    <div class="api-name font-weight-bold">{{ row.item.name }}</div>
                    <div class="api-url text-muted small">{{ row.item.url }}</div>
                    <div class="api-time text-muted small">
                      <i class="far fa-clock" style="margin-right: 5px;"></i>
                      {{ formatDate(row.item.time) }}
                    </div>
                  </div>
                </template>

                <template #cell(phuongThuc)="row">
                  {{ row.item.phuongThuc?.name || "Không xác định" }}
                </template>

                <template #cell(trangThai)="row">
                  <span
                    :class="{ 
                      'text-danger': row.item.trangThai?.name !== 'Thành công', 
                      'text-success': row.item.trangThai?.name === 'Thành công' 
                    }"
                  >
                    {{ row.item.trangThai?.name || "N/A" }}
                    - {{ row.item.code }}
                  </span>
                </template>

                <template #cell(actions)="row">
                  <b-button 
                    size="sm" 
                    variant="outline-primary" 
                    @click="handleUpdate(row.item)"
                    :disabled="isLoading"
                  >
                    <i class="fas fa-pencil-alt text-success"></i>
                  </b-button>
                  <b-button 
                    size="sm" 
                    variant="outline-danger" 
                    @click="confirmDelete(row.item)"
                    :disabled="isLoading"
                    class="ml-1"
                  >
                    <i class="fas fa-trash-alt"></i>
                  </b-button>
                </template>
              </b-table>
            </div>
          </b-card>
        </div>
      </div>
    </div>
  </Layout>
</template>

<style scoped>
.table th {
  text-align: center;
}

.datatables thead tr th:after {
  content: "\f0140";
  position: absolute;
  right: 0;
  top: 14px;
  opacity: 0.3;
  font: normal normal normal 24px / 1 "Material Design Icons";
}

.combined-info {
  line-height: 1.4;
}

.api-name {
  font-size: 1rem;
}

.api-url, .api-time {
  font-size: 0.85rem;
}

.btn-outline {
  margin: 0 2px;
}
</style>