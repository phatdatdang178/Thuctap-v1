<script>
import { mapState } from "vuex";
import { required } from "vuelidate/lib/validators";
import Vue from "vue";
import { VueRecaptcha } from 'vue-recaptcha';
import minLength from "vuelidate/lib/validators/minLength";
import maxLength from "vuelidate/lib/validators/maxLength";
import sameAs from "vuelidate/lib/validators/sameAs";
import LetterCube from "@/components/LetterCube";
import Countdown from "@/components/Countdown/countdown";
import axios from "axios";
import moment from "moment";

const defaultProps = {
  hex: '#2b569a',
};


/**
 * Crypto ICO-landing page
 */
export default {
  components: {},
  data() {
    return {
      showButton: false,
      start: "",
      end: "",
      interval: "",
      days: "",
      minutes: "",
      hours: "",
      seconds: "",
      starttime: "Nov 5, 2020 15:37:25",
      endtime: "Dec 31, 2021 16:37:25",
      showModal: false,
      showRegisterModal: false,
      showForgotModal: false,
      showNotify: false,
      email: "",
      password: "",
      submitted: false,
      authError: null,
      tryingToLogIn: false,
      isAuthError: false,
      capcha: null,
      modelRegister: {
        firstName: null,
        lastName: null,
        userName: null,
        soDienThoai: null,
        email: null,
        password: null,
        confirmPassword: null,
        phoneNumber: null,
        emailAddress: null
      },
      modelAuth: {
        isAuthError: false,
        message: null
      },
      model: {
        userName: "",
        password: ""
      },
      currentUserAuth: null,
      isShow: false,
      url: `${process.env.VUE_APP_API_URL}filesminio/view/`,
      showPDF: false,
      pdfID: '',
      toltip: null,
      file: {
        id: null,
        fileId: null,
        fileName: null,
        ext: ".pdf"
      },
      otpShow: false,
      verifyOpt: {
        sender: "du.tranphuoc@gmail.com",
        receiver: "0836980284",
        token: ""
      },
      sendSmsOtp: {
        sender: "du.tranphuoc@gmail.com",
        receiver: "0836980284",
        applicationTitle: "Test DGov"
      },
      accessToken: null,
      tempUser: null,
      showButtonSendOTP: true,
      event: null,
      treeView: [],
      lienHe: {},
      suKien: [],
      idMenu: "",
      urlHeader: ""
    };
  },
  validations: {
    model: {
      userName: {
        required,
      },
      password: {
        required
      }
    }
  },
  created() {
    let authUser = localStorage.getItem("auth-user");
    if (authUser) {
      let jsonUserCurrent = JSON.parse(authUser);
      this.currentUserAuth = jsonUserCurrent;
      console.log("CURRENT USER AUTH  created : ", this.currentUserAuth)
    }
  },
  destroyed() {
    window.removeEventListener('scroll', this.handleScroll);
  },
  mounted() {
    window.addEventListener('scroll', this.handleScroll);
  },
  computed: {
    ...mapState('snackBarStore', ['notify', 'registerModal'])
  },
  methods: {
    async getThongTinHeader() {
      let promise = this.$store.dispatch("headerStore/getAll")
      return promise.then(resp => {
        if (resp.data == null) {
          return []
        } else {
          if (resp.data != null) {
            this.urlHeader = this.url + resp.data.file.fileId;
          }
        }
      })
    },
    handleScroll() {
      if (window.scrollY > 500) {
        this.showButton = true;
      } else {
        this.showButton = false;
      }
    },
    scrollToTop() {
      window.scrollTo({
        top: 0,
        behavior: 'smooth'
      });
    },
    toggleMenu() {
      document.getElementById("topnav-menu-content").classList.toggle("show");
    },
    nextSlide() {
      this.$refs.carousel.goToPage(this.$refs.carousel.getNextPage());
    },
    prevSlide() {
      this.$refs.carousel.goToPage(this.$refs.carousel.getPreviousPage());
    },
    async GetTreeList() {
      console.log("LOG TREEVIEW : ")
      await this.$store.dispatch("menuCongDanStore/getTreeList").then((res) => {
        this.treeView = res.data;
        // console.log("LOG TREEVIEW : ", this.treeView)
      })
    },
    async getTreeFlatten() {
      await this.$store.dispatch("menuCongDanStore/getTreeFlatten").then((res) => {
        localStorage.setItem('flatten-menu', JSON.stringify(res.data));

      })
    },
    async getLienHe() {
      await this.$store.dispatch("lienheStore/getAll").then((res) => {
        // console.log("LIEN HE: ", res.data)
        this.lienHe = res.data;
      })
    },
    async getSuKien() {
      await this.$store.dispatch("suKienStore/getAll").then((res) => {
        console.log("SU KIEN: ", res.data)
        this.suKien = res.data;
        if (res.data && res.data.length > 0) {
          // Lấy phần tử cuối cùng của mảng
          const lastIndex = res.data.length - 1;
          this.suKien = res.data[lastIndex];
        }
      })
    },
    handleGetIdMenu(item) {
      if (item.id != this.idMenu) {
        if (item.link.indexOf("/{id}") < 0 && item.level === 0) {
          //   console.log("LOG ROUTER IF LAYOUT 2 : ", item)
          this.idMenu = item.id;
          //  console.log("LOG ITEM : ", item.link)
          this.$router.push(item.link);
        } else if (item.link.indexOf("/{id}") > 0 && item.level === 0) {
          this.idMenu = item.id;
          //  console.log("LOG ROUTER IF ELSE  LAYOUT 2 : ", item.link.replace("{id}",  item.id))
          this.$router.push(item.link.replace("{id}", item.id));
        } else {
          //  console.log("LOG ROUTER ELSE LAYOUT 2 : ", item.link +   item.id)
          this.idMenu = item.id;
          this.$router.push(item.link + "/" + item.id);
        }
      }
    },
    async Login(e) {
      e.preventDefault();
      this.submitted = true;
      this.$v.$touch();
      if (this.$v.model.$invalid) {
        return;
      } else {
        let loader = this.$loading.show({
          container: this.$refs.formContainer,
        });
        await this.$store.dispatch("authStore/login", this.model).then((res) => {
          if (res.code === 0) {
            this.showModal = false;
            if (res.data.menu != null) {
              window.location.href = '/tai-khoan'
            }
            localStorage.setItem('auth-user', JSON.stringify(res.data));
            localStorage.setItem("user-token", JSON.stringify(res.data.accessToken));
            Vue.prototype.$auth_token = res.data.token;
            this.model = {
              username: null,
              password: null
            }
            this.$notify({
              group: "foo",
              title: "Thông báo",
              type: "success",
              text: res.message,
            });
            let authUser = localStorage.getItem("auth-user");
            if (authUser) {

              let jsonUserCurrent = JSON.parse(authUser);
              //    console.log("LOG SUBMIT :  ", jsonUserCurrent)
              this.currentUserAuth = jsonUserCurrent;
              //     console.log("LOG SUBMIT 1231 :  ", this.currentUserAuth)
            }
            //     this.$store.dispatch("authStore/setCurrentUser", res.data.accessToken);
          } else {
            this.$notify({
              group: "foo",
              title: "Thông báo",
              type: "warn",
              text: res.message,
            });
          }
        });
        loader.hide();
      }
      this.submitted = false;
    },
    logout() {
      console.log("work")
      // eslint-disable-next-line no-unused-vars
      var userLocalStorage = localStorage.getItem("user-token");
      if (userLocalStorage) {
        localStorage.removeItem("user-token");
        localStorage.removeItem("auth-user");
        Vue.prototype.$auth_token = null;
        this.$store.dispatch("authStore/setCurrentUser", null);
        window.location.href = "/"
        return;
      }
    },
    handlePush(path) {
      let pathUrl = path ?? "/"
      if (pathUrl != window.location.pathname) {
        this.$router.push(pathUrl)
      }
    },
  },
  watch: {
    registerModal: {
      deep: true,
      handler(val) {

      }
    },
    modelRegister: {
      deep: true,
      handler(val) {

      }
    }
  }
};
</script>
<template>
  <div class="position-relative">
    <div class="navbar-expand-lg fixed-top">
      <nav class="navbar pd-0 bg-menu" id="navbar" style="padding: 0px;">
        <div class="cs-navbar-header menu">
          <button type="button"
            class="btn btn-sm px-3 font-size-16 d-lg-none header-item r-guiphananh btn-collapse bd pd"
            data-toggle="collapse" data-target="#topnav-menu-content" @click="toggleMenu()">
            <i class="mdi mdi-format-align-justify pd-5" style="color: #fff"></i>
          </button>
          <div class="collapse navbar-collapse" id="topnav-menu-content">
            <span style="padding: 0px 20px;font-size: 20px; color: #fff;">
              <a href="https://hoasadec.com.vn/" style="color: #fff;">Hệ thống giám sát api</a></span>
            <div class="book-hoa">
              <router-link :to="{
                path: `/book`,
              }" target="_blank">

              </router-link>
            </div>
            <ul class="navbar-nav" id="topnav-menu" v-scroll-spy-active="{ selector: 'a.nav-link' }">
              <div v-for="(item, index) in treeView" :key="index" class="dropdown">
                <button class="btn" type="button" id="dropdownMenuButton" data-mdb-toggle="dropdown"
                  aria-expanded="false" @click="handleGetIdMenu(item)">
                  <a class="nav-link fs-14"><strong>{{ item.label }}</strong></a>
                </button>
                <ul class="dropdown-menu" aria-labelledby="dropdownMenuButton" v-if="item.children">
                  <div v-for="(item, index) in item.children" :key="index">
                    <button @click="handleGetIdMenu(item)">
                      <a class="nav-link fs-14"><strong>{{ item.label }}</strong></a>
                    </button>
                  </div>
                </ul>
              </div>
              <div class="ms-lg-2 text-white" style="padding: 8px;">
                <b-dropdown v-if="currentUserAuth" right variant="black" toggle-class="header-item"
                  menu-class="dropdown-menu-end menu-congdan" style="width: 100%">
                  <template v-slot:button-content>
                    <div style="display: flex; align-items: flex-start; justify-content: center">
                      <div class="d-flex align-items-center">

                      </div>
                      <div v-if="currentUserAuth.avatar != null">
                        <img class="rounded-circle header-profile-user" :src="url + `${currentUserAuth.avatar}`"
                          alt="Avatar" />
                      </div>
                      <div v-else>
                        <img class="rounded-circle header-profile-user" src="@/assets/images/logo-con.png"
                          alt="Avatar" />
                      </div>

                    </div>
                  </template>
                  <!-- item-->
                  <b-dropdown-item style="width: 100%;">
                    <span class="d-xl-inline-block ms-1">
                      <div v-if="currentUserAuth">
                        <div
                          style="display: flex; flex-direction: column; justify-content: left; align-items: flex-start">
                          <div class="text-black font-weight-bold" style="white-space: initial;">{{
                            currentUserAuth.fullName }}</div>
                          <div style="font-size: 10px; color: black;">@{{ currentUserAuth.userName }}</div>
                        </div>
                      </div>
                      <div v-else class="text-black font-weight-bold">
                        Khách
                      </div>
                    </span>
                  </b-dropdown-item>
                  <hr class="my-1">
                  <b-dropdown-item v-if="currentUserAuth != null && currentUserAuth.menu != null"
                    @click="handlePush('/tai-khoan')" style="width: 100%">
                    <i class="bx bx-user font-size-16 align-middle me-1"></i>
                    Vào trang quản trị
                  </b-dropdown-item>
                  <b-dropdown-item @click="handlePush('/thong-tin-ca-nhan')" style="width: 100%">
                    <i class="bx bx-user font-size-16 align-middle me-1"></i>
                    Thông tin cá nhân
                  </b-dropdown-item>
                  <a @click="logout" href="javascript:void(0)" class="dropdown-item font-weight-bold"
                    style="color: #faf150;">
                    <i class="bx bx-power-off font-size-16 align-middle me-1" style="color: #faf150;"></i>
                    Đăng xuất
                  </a>
                </b-dropdown>
                <div v-else class="dangnhap">
                  <button type="button" style="padding: 8px 5px;"
                    v-on:click="showModal = true, showRegisterModal = false, showForgotModal = false"
                    class="btn w-xs btn-login">ĐĂNG
                    NHẬP
                  </button>
                </div>

              </div>
              <!-- <div style="padding: 10px;">
              <button type="button" style="float: right;padding: 8px 5px;"
                        v-on:click="showModal = true, showRegisterModal = false, showForgotModal= false"
                        class="btn w-xs btn-login">ĐĂNG NHẬP
              </button>
            </div> -->
            </ul>
          </div>
        </div>
      </nav>

    </div>
    <div v-scroll-spy>
      <slot />
      <div style="padding: 0px !important;">
        <img src="@/assets/images/bg-footer.png" alt="" style="width: 100%;">
      </div>
      <footer class="landing-footer bg-footer bg-menu">
        <div class="container">
          <div style="color: #fff; text-align: center; font-size: 15px;">
            Copyright © 2023 Sở Thông tin và Truyền thông

          </div>
        </div>

        <!-- ĐĂNG NHẬP -->
        <b-modal v-model="showModal" title="Thông tin đăng nhập" title-class="text-black font-18" body-class="p-3"
          hide-footer hide-header centered no-close-on-backdrop size="md" style="padding: 0px">
          <Transition name="fade" mode="out-in">
            <div v-if="!showRegisterModal && !showForgotModal" class="row justify-content-center">
              <div class="col-md-12">
                <div class="card overflow-hidden" style="padding: 0px; margin-bottom: 0px; box-shadow: none">
                  <div class="bg-soft bg-primary bg-login">
                  </div>
                  <div class="card-body pt-0" style="padding: 10px 5px">
                    <div>
                      <router-link to="/" style="display: flex; justify-content: center">
                        <div class="avatar-md profile-user-wid mb-4">
                          <span class="avatar-title rounded-circle bg-light">
                            <img src="@/assets/images/4.png" alt height="80" style="padding: 5px;" />
                          </span>
                        </div>
                      </router-link>
                    </div>
                    <b-form class="p-0" @submit.prevent="Login" ref="formContainer">
                      <h4 style="text-align: center; margin-bottom: 20px"> Thông tin đăng nhập</h4>
                      <b-alert v-model="modelAuth.isAuthError" variant="danger" class="mt-3" dismissible>{{
                        modelAuth.message }}
                      </b-alert>
                      <b-form-group class="mb-3" id="input-group-1" label="Tài khoản" label-for="input-1">
                        <b-form-input id="input-1" v-model="model.userName" type="text" placeholder="Nhập tên đăng nhập"
                          :class="{ 'is-invalid': submitted && $v.model.userName.$error }"></b-form-input>
                        <div v-if="submitted && $v.model.userName.$error" class="invalid-feedback">
                          <span v-if="!$v.model.userName.required">Tài khoản không được trống.</span>
                        </div>
                      </b-form-group>

                      <b-form-group class="mb-3" id="input-group-2" label="Mật khẩu" label-for="input-2">
                        <b-form-input id="input-2" v-model="model.password" type="password" placeholder="Nhập mật khẩu"
                          :class="{ 'is-invalid': submitted && $v.model.password.$error }"></b-form-input>
                        <div v-if="submitted && !$v.model.password.required" class="invalid-feedback">
                          Mật khẩu không được để trống!
                        </div>
                      </b-form-group>
                      <div class="row">
                        <div class="mt-1  col-md-6">
                          <b-button type="button" variant="danger" class="btn-block w-100"
                            v-on:click="showModal = false">Thoát
                          </b-button>
                        </div>
                        <div class="mt-1 col-md-6">
                          <b-button type="submit" variant="success" class="btn-block w-100">Đăng nhập
                          </b-button>
                        </div>
                      </div>

                    </b-form>
                  </div>
                  <!-- end card-body -->
                </div>
              </div>
              <!-- end col -->
            </div>
          </Transition>
        </b-modal>

      </footer>
      <!-- Footer end -->
    </div>
    <button v-if="showButton" @click="scrollToTop" id="backToTopBtn" class="btn-back">
      <i class="mdi mdi-chevron-double-up" aria-hidden="true" style="font-size: 20px;"></i>
    </button>
    <!-- <div class="btn-lk">
      <router-link
          :to="{
            path: `/book`,
            }"
          target="_blank"
      >
        <i class="fa fa-book" aria-hidden="true" style="font-size: 50px; color: rgb(216 38 110);"></i>
      </router-link>
    </div> -->
  </div>
</template>
