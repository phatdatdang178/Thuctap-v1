<script>
import Layout from "@/layouts/main";
import PageHeader from "@/components/page-header";
import Multiselect from "vue-multiselect";
import DatePicker from "vue2-datepicker";


export default {
  components: { Layout, PageHeader, Multiselect },
  data() {
    return {
      title: "Gọi api",
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

      } catch (error) {
        console.error("Lỗi khi gọi API:", error);
        this.$bvToast.toast("Lỗi khi gọi API", { variant: "danger" });
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
      <div class="row justify-content-center">
        <div class="col-lg-8 col-md-10 col-sm-12">
          <b-card>
            <b-form @submit.prevent="create">
              <b-form-group label="Tên API">
                <b-form-input v-model="apiRequest.name" required></b-form-input>
              </b-form-group>
              <div class="row mt-3">
                <b-form-group class="col-md-4 col-sm-12" label="Phương thức API">
                  <multiselect v-model="selectedPhuongThuc" :options="listPhuongThuc" label="name" track-by="name" placeholder="Chọn phương thức API" />
                </b-form-group>
                <b-form-group class="col-md-8 col-sm-12" label="URL">
                  <b-form-input v-model="apiRequest.url" required></b-form-input>
                </b-form-group>
              </div>
              <b-form-group class="mt-3" label="Body (JSON)">
                <b-form-textarea v-model="apiRequest.body" rows="5" required></b-form-textarea>
              </b-form-group>
              <div class="text-center">
                <b-button class="mt-3 cs-btn-primary" type="submit" variant="primary" >Gọi API</b-button>
              </div>
            </b-form>
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

.multiselect_phuongthuc {
  width: 100%;
  height: auto;
}

.multiselect__select {
  display: none;
}

.multiselect__single {
  font-weight: bold;
}

/* Màu cam khi aria-activedescendant là null-0 */
.multiselect__input[aria-activedescendant="null-1"]+.multiselect__single {
  color: orange;
}

/* Màu khác khi aria-activedescendant khác null-0 */
.multiselect__input[aria-activedescendant="null-0"]+.multiselect__single {
  color: rgb(10, 82, 10);
}

.multiselect__option::before,
.multiselect__option::after {
  display: none;
}
</style>