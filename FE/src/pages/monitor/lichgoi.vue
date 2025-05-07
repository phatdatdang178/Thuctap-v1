<script>
import Layout from "@/layouts/main";
import PageHeader from "@/components/page-header";
import { monitorModel } from "@/models/monitorModel";
import { pagingModel } from "@/models/pagingModel";
import {notifyModel} from "@/models/notifyModel";


export default {
  components: { Layout, PageHeader },
  data() {
    return {
      title: "Lịch gọi API",
      items: [
        { text: "Monitor", href: "/monitor" },
        { text: "Gọi API & Lịch trình", active: true }
      ],
      fields: [
        {
          key: "thongTin",
          label: "Thông tin API",
          thStyle: "text-align:center",
          thClass: "hidden-sortable"
        },
        {
          key: "trangThai",
          label: "Trạng thái",
          sortable: true,
          thStyle: "text-align:center"
        },
        {
          key: "phuongThuc",
          label: "Phương thức",
          sortable: false,
          thClass: "hidden-sortable",
          thStyle: "text-align:center"
          
        },
        {
          key: "actions",
          label: "Thao tác",
          thClass: "hidden-sortable",
          tdClass: "text-center"
        }
      ],
      model: monitorModel.baseJson(),
      searchQuery: "",
      perPage: 10,
      pageOptions: [5, 10, 25, 50, 100],
      currentPage: 1,
      sortBy: "time",
      sortDesc: true,
      filter: null,
      filterOn: [],
      isLoading: false,
      showModal: false,
      showDeleteModal: false,
      deletingId: null,
      listPhuongThuc: [],
      totalRows: 0,
      numberOfElement: 0,
      pagination: pagingModel.baseJson()
    };
  },

  methods: {
    formatDateTime(datetime) {
    if (!datetime) return "N/A";
    
    // Sử dụng UTC methods để không bị ảnh hưởng bởi múi giờ local
    const date = new Date(datetime);
    const day = date.getUTCDate().toString().padStart(2, '0');
    const month = (date.getUTCMonth() + 1).toString().padStart(2, '0');
    const year = date.getUTCFullYear();
    const hours = date.getUTCHours().toString().padStart(2, '0');
    const minutes = date.getUTCMinutes().toString().padStart(2, '0');
    const seconds = date.getUTCSeconds().toString().padStart(2, '0');
    
    return `${day}/${month}/${year} ${hours}:${minutes}:${seconds}`;
    
  },  
    async getDropdownData() {
      try {
        const res = await this.$store.dispatch("monitorStore/getMethods");
        if (res.code === 0) {
          this.listPhuongThuc = res.data || [];
        }
      } catch (error) {
        console.error("Lỗi lấy danh sách phương thức:", error);
      }
    },

      async exportExcel() {
    try {
      this.isLoading = true;
      const response = await this.$store.dispatch("monitorStore/exportExcel");
      const blob = new Blob([response], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
      });
      const link = document.createElement("a");
      link.href = window.URL.createObjectURL(blob);
      link.download = "LichSuGoiAPI.xlsx";
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      // Thay thế $bvToast bằng snackBarStore
      this.$store.dispatch("snackBarStore/addNotify", 
        notifyModel.addMessage({code: 0, message: "Xuất Excel thành công"})
      );
    } catch (err) {
      console.error("Lỗi xuất Excel:", err);
      this.$store.dispatch("snackBarStore/addNotify", 
        notifyModel.addMessage({code: -1, message: "Lỗi khi xuất Excel"})
      );
    } finally {
      this.isLoading = false;
    }
  },

    confirmDelete(item) {
      this.model = { ...item };
      this.$bvModal
        .msgBoxConfirm("Bạn có chắc chắn muốn xóa bản ghi này?", {
          title: "Xác nhận xóa",
          size: "sm",
          okVariant: "danger",
          okTitle: "Xóa",
          cancelTitle: "Hủy",
          centered: true
        })
        .then(value => {
          if (value) this.handleDelete();
        });
    },

      async handleDelete() {
    if (!this.model._id) return;
    this.deletingId = this.model._id;
    try {
      const res = await this.$store.dispatch("monitorStore/deleted", {
        _id: this.model._id
      });
      if (res.code === 0) {
        // Thay thế $bvToast bằng snackBarStore
        this.$store.dispatch("snackBarStore/addNotify", 
          notifyModel.addMessage({code: 0, message: "Xóa thành công"})
        );
        this.refreshTable();
      } else {
        this.$store.dispatch("snackBarStore/addNotify", 
          notifyModel.addMessage(res)
        );
      }
    } catch (err) {
      console.error("Lỗi xóa:", err);
      this.$store.dispatch("snackBarStore/addNotify", 
        notifyModel.addMessage({code: -1, message: "Lỗi hệ thống"})
      );
    } finally {
      this.deletingId = null;
    }
  },

    refreshTable() {
      this.currentPage = 1;
      this.$refs.apiTable.refresh();
    },

    myProvider (ctx) {
      const params = {
        start: ctx.currentPage,
        limit: ctx.perPage,
        content: this.filter,
        sortBy: ctx.sortBy,
        sortDesc: ctx.sortDesc,
      }
      this.loading = true
      try {
        let promise =  this.$store.dispatch("monitorStore/getPagingParams", params)
        return promise.then(resp => {
          let items = resp.data.data
          this.totalRows = resp.data.totalRows
          this.numberOfElement = resp.data.data.length
          this.loading = false
          return items || []
        })
      } finally {
        this.loading = false
      }
    },
  },
  mounted() {
    this.getDropdownData();
  }
};
</script>

<template>
  <Layout>
    <div class="container-fluid">
      <PageHeader :title="title" :items="items" />

      <b-card>
        <div class="d-flex justify-content-between align-items-center mb-3">
          <div class="search-box me-2 mb-2 d-inline-block">
                  <div class="position-relative">
                    <input
                        v-model = "filter"
                        type="text"
                        class="form-control"
                        placeholder="Tìm kiếm ..."
                    />
                    <i class="bx bx-search-alt search-icon"></i>
                  </div>
                </div>

          <div class="d-flex align-items-center">
            <span class="me-2">Hiển thị</span>
            <b-form-select
              class="form-select-sm"
              v-model="perPage"
              :options="pageOptions"
              style="width: 70px"
              @change="refreshTable"
            />
            <span class="mx-2">dòng</span>
            <b-button 
              class="cs-btn-primary"
              variant="success"
              @click="exportExcel"
              :disabled="isLoading"
            >
              <i class="fas fa-file-excel"></i>
              {{ isLoading ? "Đang xử lý..." : "Xuất Excel" }}
            </b-button>
          </div>
        </div>

        <b-table
          ref="apiTable"
          hover
          striped
          responsive
          show-empty
          primary-key="_id"
          :filter-included-fields="filterOn"
          :filter="filter"
          :items="myProvider"
          :fields="fields"
          :per-page="perPage"
          :current-page="currentPage"
          :sort-by.sync="sortBy"
          :sort-desc.sync="sortDesc"
          :busy.sync="isLoading"
          :tbody-tr-class="row => row.trangThai === 'FAILED' ? 'table-danger' : ''"
          @sort-changed="refreshTable"
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
                <i class="far fa-clock me-1"></i>
                {{formatDateTime(row.item.time) }}
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
              v-b-tooltip.hover
              :title="'Mã phản hồi: ' + row.item.code"
            >
              <i
                v-if="row.item.trangThai?.name !== 'Thành công'"
                class="fas fa-exclamation-triangle me-1"
              ></i>
              {{ row.item.trangThai?.name || "N/A" }} - {{ row.item.code }}
            </span>
          </template>

          <template #cell(actions)="row">
            <b-button
              size="sm"
              variant="outline-danger"
              @click="confirmDelete(row.item)"
              :disabled="deletingId === row.item._id"
            >
              <i class="fas fa-trash-alt"></i>
            </b-button>
          </template>
        </b-table>

        <div class="d-flex justify-content-between align-items-center mt-3">
          <div>Hiển thị {{ numberOfElement }} / {{ totalRows }} dòng</div>
          <b-pagination
            v-model="currentPage"
            :total-rows="totalRows"
            :per-page="perPage"
            class="pagination pagination-rounded mb-0"
            @change="refreshTable"
          />
        </div>
      </b-card>
    </div>
  </Layout>
</template>

<style scoped>
.table th {
  text-align: center;
}

.combined-info {
  line-height: 1.4;
}

.api-name {
  font-size: 1rem;
}

.api-url,
.api-time {
  font-size: 0.85rem;
}
.table th {
  text-align: center;
}

.combined-info {
  line-height: 1.4;
}

.api-name {
  font-size: 1rem;
}

.api-url,
.api-time {
  font-size: 0.85rem;
}


.td-actions {
  text-align: center;
  vertical-align: middle;
}

::v-deep td.td-actions {
  display: flex;
  justify-content: center;
  align-items: center;
}
</style>