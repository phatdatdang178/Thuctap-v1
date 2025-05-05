<script>
import Layout from "@/layouts/main";
import PageHeader from "@/components/page-header";
import Multiselect from "vue-multiselect";
import DatePicker from "vue2-datepicker";
import { notifyModel } from "@/models/notifyModel";
import { required } from "vuelidate/lib/validators";

export default {
  components: { Layout, PageHeader, Multiselect },
  data() {
    return {
      title: "Gọi API",
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
      submitted: false,
      apiRequest: {
        method: "",
        name: "",
        url: "",
        body: '{ "start": 0, "limit": 5, "serviceId": "string" }'
      },
      listPhuongThuc: [],
      selectedPhuongThuc: null,
      searchQuery: "",
      perPage: 10,
      currentPage: 1,
      urlError: null,
      jsonError: null
    };
  },

  validations: {
    apiRequest: {
      name: { required },
      url: { required },
      body: { required }
    },
    selectedPhuongThuc: { required }
  },

  methods: {
    async create() {
      this.submitted = true;
      this.$v.$touch();
      
      // Reset error messages
      this.urlError = null;
      this.jsonError = null;

      // Validate required fields
      if (this.$v.$invalid) {
        this.$store.dispatch("snackBarStore/addNotify", 
          notifyModel.addMessage({code: -1, message: "Vui lòng điền đầy đủ thông tin bắt buộc"})
        );
        return;
      }

      // Validate URL format
      if (!this.validateUrl(this.apiRequest.url)) {
        return;
      }

      // Validate JSON body
      if (!this.validateJson(this.apiRequest.body)) {
        return;
      }

      try {
        const requestData = {
          name: this.apiRequest.name,
          url: this.apiRequest.url,
          phuongThuc: { name: this.selectedPhuongThuc.name.toUpperCase() },
          bodyParams: JSON.stringify(JSON.parse(this.apiRequest.body), null, 2)
        };

        const res = await this.$store.dispatch("monitorStore/create", requestData);
        this.$store.dispatch("snackBarStore/addNotify", notifyModel.addMessage(res));

        // Reset form if success
        if (res.code === 0) {
          this.resetForm();
        }
      } catch (error) {
        console.error("Lỗi khi gọi API:", error);
        this.$store.dispatch("snackBarStore/addNotify", 
          notifyModel.addMessage({code: -1, message: "Lỗi hệ thống khi gọi API"})
        );
      }
    },

    validateUrl(url) {
      this.urlError = null;
      if (!url) return true;
      
      try {
        new URL(url);
        return true;
      } catch (e) {
        this.urlError = "URL phải có định dạng hợp lệ (ví dụ: https://example.com)";
        this.$store.dispatch("snackBarStore/addNotify", 
          notifyModel.addMessage({code: -1, message: this.urlError})
        );
        return false;
      }
    },

    validateJson(jsonStr) {
      this.jsonError = null;
      try {
        JSON.parse(jsonStr);
        return true;
      } catch (e) {
        this.jsonError = "Nội dung Body phải là JSON hợp lệ";
        this.$store.dispatch("snackBarStore/addNotify", 
          notifyModel.addMessage({code: -1, message: this.jsonError})
        );
        return false;
      }
    },

    resetForm() {
      this.apiRequest = {
        method: "",
        name: "",
        url: "",
        body: '{ "start": 0, "limit": 5, "serviceId": "string" }'
      };
      this.selectedPhuongThuc = null;
      this.submitted = false;
      this.$v.$reset();
      this.urlError = null;
      this.jsonError = null;
    },

    async getDropdownData() {
      try {
        const res = await this.$store.dispatch("commonStore/getAll", "DM_PHUONGTHUC");
        if (res.code === 0) {
          this.listPhuongThuc = res.data || [];
        } else {
          this.$store.dispatch("snackBarStore/addNotify", notifyModel.addMessage(res));
        }
      } catch (error) {
        console.error("Lỗi khi lấy danh sách phương thức API:", error);
        this.$store.dispatch("snackBarStore/addNotify", 
          notifyModel.addMessage({code: -1, message: "Lỗi khi lấy danh sách phương thức API"})
        );
      }
    }
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
            <b-form @submit.prevent="create" novalidate>
              <!-- Tên API -->
              <b-form-group label="Tên API">
                <b-form-input 
                  v-model="apiRequest.name" 
                  :class="{ 'is-invalid': submitted && $v.apiRequest.name.$error }"
                  placeholder="Nhập tên API"
                />
                <div v-if="submitted && $v.apiRequest.name.$error" class="invalid-feedback">
                  <span>Tên API không được để trống</span>
                </div>
              </b-form-group>

              <div class="row mt-3">
                <!-- Phương thức API -->
                <b-form-group class="col-md-4 col-sm-12" label="Phương thức API">
                  <multiselect 
                    v-model="selectedPhuongThuc" 
                    :options="listPhuongThuc" 
                    label="name" 
                    track-by="name"
                    placeholder="Chọn phương thức"
                    :class="{ 'is-invalid': submitted && $v.selectedPhuongThuc.$error }"
                  />
                  <div v-if="submitted && $v.selectedPhuongThuc.$error" class="invalid-feedback">
                    <span>Phương thức không được để trống</span>
                  </div>
                </b-form-group>

                <!-- URL -->
                <b-form-group class="col-md-8 col-sm-12" label="URL">
                  <b-form-input 
                    v-model="apiRequest.url" 
                    :class="{ 'is-invalid': (submitted && $v.apiRequest.url.$error) || urlError }"
                    placeholder="Nhập URL API"
                    @blur="validateUrl(apiRequest.url)"
                  />
                  <div v-if="submitted && $v.apiRequest.url.$error" class="invalid-feedback">
                    <span>URL không được để trống</span>
                  </div>
                  <div v-if="urlError" class="invalid-feedback">
                    {{ urlError }}
                  </div>
                </b-form-group>
              </div>

              <!-- Body JSON -->
              <b-form-group class="mt-3" label="Body (JSON)">
                <b-form-textarea 
                  v-model="apiRequest.body" 
                  rows="5" 
                  :class="{ 'is-invalid': (submitted && $v.apiRequest.body.$error) || jsonError }"
                  placeholder="Nhập nội dung JSON"
                  @blur="validateJson(apiRequest.body)"
                />
                <div v-if="submitted && $v.apiRequest.body.$error" class="invalid-feedback">
                  <span>Nội dung Body không được để trống</span>
                </div>
                <div v-if="jsonError" class="invalid-feedback">
                  {{ jsonError }}
                </div>
              </b-form-group>

              <div class="text-center mt-4">
                <b-button type="submit" variant="primary" class="cs-btn-primary mr-2">Gọi API</b-button>
                
              </div>
            </b-form>
          </b-card>
        </div>
      </div>
    </div>
  </Layout>
</template>

<style scoped>
.is-invalid {
  border-color: #dc3545 !important;
}

.invalid-feedback {
  display: block;
  color: #dc3545;
  font-size: 0.875rem;
  margin-top: 0.25rem;
}

.is-invalid >>> .multiselect__tags {
  border-color: #dc3545 !important;
}
form:invalid {
  border: none !important;
}
.table th {
  text-align: center;
}

.cs-btn-primary {
  background-color: #0052D4;
  border-color: #0052D4;
}

.cs-btn-primary:hover {
  background-color: #003d9e;
  border-color: #003d9e;
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
.multiselect__input[aria-activedescendant="null-0"]+.multiselect__single {
  color: orange;
}

/* Màu khác khi aria-activedescendant khác null-0 */
.multiselect__input[aria-activedescendant="null-1"]+.multiselect__single {
  color: rgb(10, 82, 10);
}

.multiselect__option::before,
.multiselect__option::after {
  display: none;
}
</style>