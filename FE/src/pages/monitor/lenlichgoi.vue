<template>
  <Layout>
    <div class="container-fluid">
      <PageHeader :title="title" :items="items" />
      <b-card>
        <b-form @submit.prevent="schedule" novalidate>
          <!-- Tên API -->
          <b-form-group label="Tên API">
            <b-form-input 
              v-model="scheduleRequest.name" 
              :class="{ 'is-invalid': submitted && $v.scheduleRequest.name.$error }"
              placeholder="Nhập tên API"
            />
            <div v-if="submitted && $v.scheduleRequest.name.$error" class="invalid-feedback">
              <span>Tên API không được để trống</span>
            </div>
          </b-form-group>

          <div class="row">
            <!-- Phương thức API -->
            <b-form-group class="col-md-3 col-sm-12" label="Phương thức API">
              <multiselect 
                v-model="selectedSchedulePhuongThuc" 
                :options="listPhuongThuc" 
                label="name" 
                track-by="name" 
                placeholder="Chọn phương thức"
                :class="{ 'is-invalid': submitted && $v.selectedSchedulePhuongThuc.$error }"
              />
              <div v-if="submitted && $v.selectedSchedulePhuongThuc.$error" class="invalid-feedback">
                <span>Phương thức không được để trống</span>
              </div>
            </b-form-group>

            <!-- URL -->
            <b-form-group class="col-md-9 col-sm-12" label="URL">
              <b-form-input 
                v-model="scheduleRequest.url" 
                :class="{ 'is-invalid': (submitted && $v.scheduleRequest.url.$error) || urlError }"
                placeholder="Nhập URL API"
                @blur="validateUrl(scheduleRequest.url)"
              />
              <div v-if="submitted && $v.scheduleRequest.url.$error" class="invalid-feedback">
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
              v-model="scheduleRequest.body" 
              rows="5" 
              :class="{ 'is-invalid': (submitted && $v.scheduleRequest.body.$error) || jsonError }"
              placeholder="Nhập nội dung JSON"
              @blur="validateJson(scheduleRequest.body)"
            />
            <div v-if="submitted && $v.scheduleRequest.body.$error" class="invalid-feedback">
              <span>Nội dung Body không được để trống</span>
            </div>
            <div v-if="jsonError" class="invalid-feedback">
              {{ jsonError }}
            </div>
          </b-form-group>

          <!-- Giờ gọi cụ thể -->
          <b-form-group class="mt-3" label="Giờ gọi cụ thể">
            <div v-for="(time, index) in scheduleRequest.specificTimes" :key="index" class="d-flex align-items-center mb-2">
              <date-picker 
                v-model="scheduleRequest.specificTimes[index]" 
                type="time" 
                format="HH:mm" 
                value-type="format" 
                placeholder="Chọn giờ gọi" 
                class="flex-grow-1"
                :class="{ 'is-invalid': timeErrors[index] }"
                @change="validateSpecificTime(index)"
              />
              <b-button variant="danger" size="sm" class="m-2" @click="removeSpecificTime(index)">
                <i class="fas fa-times"></i>
              </b-button>
              <div v-if="timeErrors[index]" class="invalid-feedback ml-3">
                {{ timeErrors[index] }}
              </div>
            </div>
            <b-button variant="outline-primary" size="sm" @click="addSpecificTime">
              <i class="fas fa-plus mr-1"></i> Thêm giờ
            </b-button>
          </b-form-group>

          <div class="row">
            <!-- Giờ bắt đầu -->
            <div class="col-md-6 col-sm-12 mt-3">
              <label>Giờ bắt đầu</label>
              <date-picker 
                v-model="scheduleRequest.startTime" 
                type="time" 
                format="HH:mm" 
                value-type="format" 
                placeholder="Chọn giờ bắt đầu" 
                class="w-100"
                :class="{ 'is-invalid': startTimeError }"
                @change="validateStartTime"
              />
              <div v-if="startTimeError" class="invalid-feedback">
                {{ startTimeError }}
              </div>
            </div>

            <!-- Giờ kết thúc -->
            <div class="col-md-6 col-sm-12 mt-3">
              <label>Giờ kết thúc</label>
              <date-picker 
                v-model="scheduleRequest.endTime" 
                type="time" 
                format="HH:mm" 
                value-type="format" 
                placeholder="Chọn giờ kết thúc" 
                class="w-100"
                :class="{ 'is-invalid': endTimeError }"
                @change="validateEndTime"
              />
              <div v-if="endTimeError" class="invalid-feedback">
                {{ endTimeError }}
              </div>
            </div>
          </div>

          <!-- Số lần gọi -->
          <b-form-group class="mt-3" label="Số lần gọi">
            <b-form-input 
              v-model="scheduleRequest.callFrequency" 
              type="number" 
              min="1" 
              placeholder="Nhập số lần gọi"
              :class="{ 'is-invalid': (submitted && $v.scheduleRequest.callFrequency.$error) || callFrequencyError }"
              @blur="validateCallFrequency"
            />
            <div v-if="submitted && $v.scheduleRequest.callFrequency.$error" class="invalid-feedback">
              <span>Số lần gọi không được để trống</span>
            </div>
            <div v-if="callFrequencyError" class="invalid-feedback">
              {{ callFrequencyError }}
            </div>
          </b-form-group>

          <div class="d-flex justify-content-between mt-4">
            <b-button type="button" variant="outline-secondary" @click="resetForm">
              <i class="fas fa-redo mr-1"></i> Đặt lại
            </b-button>
            <b-button type="submit" variant="primary" class="cs-btn-primary">
              <i class="fas fa-calendar-plus mr-1"></i> Lên lịch API
            </b-button>
          </div>
        </b-form>
      </b-card>
    </div>
  </Layout>
</template>

<script>
import Layout from "@/layouts/main";
import PageHeader from "@/components/page-header";
import Multiselect from "vue-multiselect";
import DatePicker from "vue2-datepicker";
import { notifyModel } from "@/models/notifyModel";
import { required } from "vuelidate/lib/validators";

export default {
  components: { Layout, PageHeader, Multiselect, DatePicker },
  data() {
    return {
      title: "Lên lịch gọi API",
      items: [{ text: "Monitor", href: "/monitor" }, { text: "Gọi API & Lịch trình", active: true }],
      scheduleRequest: {
        name: "",
        url: "",
        body: '{ "start": 0, "limit": 5, "serviceId": "string" }',
        specificTimes: [""],
        startTime: "",
        endTime: "",
        callFrequency: null
      },
      listPhuongThuc: [],
      selectedSchedulePhuongThuc: null,
      submitted: false,
      urlError: null,
      jsonError: null,
      timeErrors: [null],
      startTimeError: null,
      endTimeError: null,
      callFrequencyError: null
    };
  },

  validations: {
    scheduleRequest: {
      name: { required },
      url: { required },
      body: { required },
      callFrequency: { required }
    },
    selectedSchedulePhuongThuc: { required }
  },

  methods: {
    addSpecificTime() {
      this.scheduleRequest.specificTimes.push("");
      this.timeErrors.push(null);
    },

    removeSpecificTime(index) {
      if (this.scheduleRequest.specificTimes.length > 1) {
        this.scheduleRequest.specificTimes.splice(index, 1);
        this.timeErrors.splice(index, 1);
      }
    },

    validateUrl(url) {
      this.urlError = null;
      if (!url) return;
      
      try {
        new URL(url);
      } catch (e) {
        this.urlError = "URL phải có định dạng hợp lệ (ví dụ: https://example.com)";
      }
    },

    validateJson(jsonStr) {
      this.jsonError = null;
      if (!jsonStr) return;
      
      try {
        JSON.parse(jsonStr);
      } catch (e) {
        this.jsonError = "Nội dung Body phải là JSON hợp lệ";
      }
    },

    validateSpecificTime(index) {
      this.timeErrors[index] = null;
      const time = this.scheduleRequest.specificTimes[index];
      if (!time) return;
      
      if (!/^([01]?[0-9]|2[0-3]):[0-5][0-9]$/.test(time.trim())) {
        this.timeErrors[index] = "Định dạng giờ phải là HH:mm";
      }
    },

    validateStartTime() {
      this.startTimeError = null;
      if (!this.scheduleRequest.startTime) return;
      
      if (!/^([01]?[0-9]|2[0-3]):[0-5][0-9]$/.test(this.scheduleRequest.startTime.trim())) {
        this.startTimeError = "Định dạng giờ phải là HH:mm";
      }
    },

    validateEndTime() {
      this.endTimeError = null;
      if (!this.scheduleRequest.endTime) return;
      
      if (!/^([01]?[0-9]|2[0-3]):[0-5][0-9]$/.test(this.scheduleRequest.endTime.trim())) {
        this.endTimeError = "Định dạng giờ phải là HH:mm";
      }
      
      // Validate end time > start time if both exist
      if (this.scheduleRequest.startTime && this.scheduleRequest.endTime) {
        const start = this.scheduleRequest.startTime;
        const end = this.scheduleRequest.endTime;
        if (start >= end) {
          this.endTimeError = "Giờ kết thúc phải sau giờ bắt đầu";
        }
      }
    },

    validateCallFrequency() {
      this.callFrequencyError = null;
      if (this.scheduleRequest.callFrequency && this.scheduleRequest.callFrequency < 1) {
        this.callFrequencyError = "Số lần gọi phải lớn hơn 0";
      }
    },

    async schedule() {
      this.submitted = true;
      this.$v.$touch();
      
      // Validate all fields
      this.validateUrl(this.scheduleRequest.url);
      this.validateJson(this.scheduleRequest.body);
      this.scheduleRequest.specificTimes.forEach((_, index) => this.validateSpecificTime(index));
      this.validateStartTime();
      this.validateEndTime();
      this.validateCallFrequency();

      // Check if any errors exist
      const hasErrors = this.$v.$invalid || 
        this.urlError || 
        this.jsonError || 
        this.timeErrors.some(e => e) || 
        this.startTimeError || 
        this.endTimeError || 
        this.callFrequencyError;

      if (hasErrors) {
        this.$store.dispatch("snackBarStore/addNotify", 
          notifyModel.addMessage({code: -1, message: "Vui lòng kiểm tra lại các thông tin nhập"})
        );
        return;
      }

      try {
        const specificTimesArray = this.scheduleRequest.specificTimes
          .filter(time => time)
          .map(time => time.trim());

        const scheduleData = {
          monitorApiModel: {
            name: this.scheduleRequest.name.trim(),
            url: this.scheduleRequest.url.trim(),
            phuongThuc: { name: this.selectedSchedulePhuongThuc.name.toUpperCase() },
            bodyParams: JSON.stringify(JSON.parse(this.scheduleRequest.body), null, 2)
          },
          ...(specificTimesArray.length > 0 && { specificTimes: specificTimesArray }),
          ...(this.scheduleRequest.startTime && { startTime: this.scheduleRequest.startTime.trim() }),
          ...(this.scheduleRequest.endTime && { endTime: this.scheduleRequest.endTime.trim() }),
          callFrequency: Number(this.scheduleRequest.callFrequency)
        };

        const res = await this.$store.dispatch("monitorStore/createSchedule", scheduleData);
        this.$store.dispatch("snackBarStore/addNotify", notifyModel.addMessage(res));

        if (res.code === 0) {
          this.resetForm();
        }
      } catch (error) {
        console.error("Lỗi khi lên lịch API:", error);
        this.$store.dispatch("snackBarStore/addNotify", 
          notifyModel.addMessage({code: -1, message: error.message || "Lỗi khi lên lịch API"})
        );
      }
    },

    resetForm() {
      this.scheduleRequest = {
        name: "",
        url: "",
        body: '{ "start": 0, "limit": 5, "serviceId": "string" }',
        specificTimes: [""],
        startTime: "",
        endTime: "",
        callFrequency: null
      };
      this.selectedSchedulePhuongThuc = null;
      this.submitted = false;
      this.$v.$reset();
      this.urlError = null;
      this.jsonError = null;
      this.timeErrors = [null];
      this.startTimeError = null;
      this.endTimeError = null;
      this.callFrequencyError = null;
    },

    async getDropdownData() {
      try {
        const res = await this.$store.dispatch("commonStore/getAll", "DM_PHUONGTHUC");
        this.listPhuongThuc = res.data || [];
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

.is-invalid >>> .mx-input {
  border-color: #dc3545 !important;
}

.cs-btn-primary {
  background-color: #0052D4;
  border-color: #0052D4;
  min-width: 150px;
}

.cs-btn-primary:hover {
  background-color: #003d9e;
  border-color: #003d9e;
}

/* Ẩn thông báo validation mặc định của trình duyệt */
form:invalid {
  border: none !important;
}

input:invalid, textarea:invalid {
  box-shadow: none !important;
}

/* Style cho multiselect */
.multiselect {
  width: 100%;
}

.multiselect__placeholder {
  color: #6c757d;
  margin-bottom: 0;
}

.multiselect__tags {
  min-height: 38px;
  border: 1px solid #ced4da;
  border-radius: 0.25rem;
}

.multiselect__option--highlight {
  background: #0052D4;
}

/* Style cho date picker */
.mx-input {
  height: 38px;
  border-radius: 0.25rem;
}

/* Responsive adjustments */
@media (max-width: 768px) {
  .d-flex.justify-content-between {
    flex-direction: column;
    gap: 10px;
  }
  
  .cs-btn-primary, button {
    width: 100%;
  }
}
</style>