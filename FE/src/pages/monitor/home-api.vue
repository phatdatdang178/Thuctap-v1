<script>
import Vue from "vue";
import { required } from "vuelidate/lib/validators";
import { notifyModel } from "@/models/notifyModel";

export default {
    data() {
        return {
            model: {
                userName: "",
                password: "",
            },
            submitted: false,
            showPassword: false,
        };
    },
    validations: {
        model: {
            userName: { required },
            password: { required }
        }
    },
    methods: {
        async Login() {
            this.submitted = true;
            this.$v.$touch();

            if (this.$v.$invalid) {
                this.$store.dispatch("snackBarStore/addNotify", 
                    notifyModel.addMessage({code: -1, message: "Vui lòng nhập đầy đủ tài khoản và mật khẩu"})
                );
                return;
            }

            let loader = this.$loading.show();
            try {
                const res = await this.$store.dispatch("authStore/login", this.model);
                if (res.code === 0) {
                    // Đăng nhập thành công
                    localStorage.setItem("auth-user", JSON.stringify(res.data));
                    localStorage.setItem("user-token", JSON.stringify(res.data.accessToken));
                    Vue.prototype.$auth_token = res.data.token;

                    // Hiện thông báo thành công
                    this.$store.dispatch("snackBarStore/addNotify", 
                        notifyModel.addMessage({code: 0, message: "Đăng nhập thành công"})
                    );

                    // Chuyển trang sau 1 giây
                    setTimeout(() => {
                        window.location.href = "/tai-khoan";
                    }, 1000);
                } else {
                    // Thông báo lỗi từ server
                    this.$store.dispatch("snackBarStore/addNotify", 
                        notifyModel.addMessage(res)
                    );
                }
            } catch (error) {
                console.error("Login error:", error);
                this.$store.dispatch("snackBarStore/addNotify", 
                    notifyModel.addMessage({code: -1, message: "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại!"})
                );
            } finally {
                loader.hide();
            }
        }
    },
};
</script>

<template>
    <div class="form-login container p-0 d-flex justify-content-center align-items-center vh-100">
        <div class="card card0">
            <div class=" d-flex flex-lg-row flex-column-reverse">
                <div class="card card1 d-flex align-items-center">
                    <div class="col-md-8 col-10 my-5">
                        <div class="text-center mb-2">
                            <img id="logo" src="@/assets/images/logoapi-rmbg.png" alt="logo">
                        </div>
                        <h3 class="mb-2 text-center heading">HỆ THỐNG GIÁM SÁT API</h3>
                        <h4 class="mb-2 text-center heading2">Đăng nhập</h4>
                        <form @submit.prevent="Login">
                            <div class="form-group mb-2">
                                <label for="userName" class="form-control-label">Tài khoản</label>
                                <input type="text" v-model="model.userName" id="userName"
                                    :class="{ 'is-invalid': submitted && $v.model.userName.$error }">
                                <div v-if="submitted && $v.model.userName.$error" class="invalid-feedback">
                                    <span v-if="!$v.model.userName.required">Tài khoản không được trống.</span>
                                </div>
                            </div>

                            <div class="form-group position-relative mb-2">
                                <label for="password" class="form-control-label">Mật khẩu</label>
                                <div class="input-wrapper">
                                    <input :type="showPassword ? 'text' : 'password'" v-model="model.password"
                                        id="password" :class="{ 'is-invalid': submitted && $v.model.password.$error }">
                                    <i :class="showPassword ? 'fas fa-eye-slash' : 'fas fa-eye'" class="eye-icon"
                                        @click="showPassword = !showPassword"></i>
                                </div>
                                <div v-if="submitted && $v.model.password.$error" class="invalid-feedback">
                                    <span v-if="!$v.model.password.required">Mật khẩu không được trống.</span>
                                </div>
                            </div>

                            <div class="row justify-content-center my-3 px-3">
                                <button type="submit" class="btn-block btn-color">Đăng nhập</button>
                            </div>
                        </form>
                    </div>
                </div>
                <div class="card card2">
                    <img class="my-auto mx-md-5 px-md-5 right" src="@/assets/images/login-image1.png" alt="">
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
/* Giữ nguyên toàn bộ phần style như cũ */
.form-login {
    color: #000;
    min-height: 100vh;
    background-image: linear-gradient(to right, #0052D4, #1CB5E0);
    background-repeat: no-repeat;
}

input,
textarea {
    background-color: #f0f0f0;
    border-radius: 50px !important;
    padding: 12px 15px 12px 15px !important;
    width: 100%;
    box-sizing: border-box;
    border: none !important;
    border: 1px solid #F3E5F5 !important;
    font-size: 16px !important;
    color: #000 !important;
    font-weight: 400;
}

input:focus,
textarea:focus {
    -moz-box-shadow: none !important;
    -webkit-box-shadow: none !important;
    box-shadow: none !important;
    border: 1px solid #0052D4 !important;
    outline-width: 0;
    font-weight: 400;
}

button:focus {
    -moz-box-shadow: none !important;
    -webkit-box-shadow: none !important;
    box-shadow: none !important;
    outline-width: 0;
}

.card {
    border-radius: 20px;
    border: none;
    width: 70%;
    margin-bottom: 0;
}

.card1 {
    width: 50%;
}

.card2 {
    width: 50%;
}

.card2 img {
    object-fit: cover;
}

#logo {
    width: 90px;
    height: 90px;
}

.heading {
    font-weight: bold;
    font-size: 40;
}

.heading2 {
    font-weight: bold;
}

::placeholder {
    color: #000 !important;
    opacity: 1;
}

.form-control-label {
    font-size: 12px;
    margin-left: 15px;
}

.btn-color {
    border-radius: 50px;
    color: #fff;
    background-image: linear-gradient(to right, #1CB5E0, #0052D4);
    padding: 15px;
    cursor: pointer;
    border: none !important;
}

.btn-color:hover {
    color: #fff;
    background-image: linear-gradient(to right, #0052D4, #1CB5E0);
}

.input-wrapper {
    position: relative;
    width: 100%;
}

.input-wrapper input {
    padding-right: 40px;
}

.eye-icon {
    position: absolute;
    right: 15px;
    top: 50%;
    transform: translateY(-50%);
    cursor: pointer;
    color: #0052D4;
    font-size: 18px;
}

.invalid-feedback {
    display: block;
    margin-top: 5px;
    color: #dc3545;
}

.is-invalid {
    border-color: #dc3545 !important;
}

@media screen and (max-width: 992px) {
    .card1 {
        width: 100%;
    }

    .card2 {
        width: 100%;
    }

    .right {
        margin-top: 100px !important;
        margin-bottom: 100px !important;
    }
}

@media screen and (max-width: 768px) {
    .container {
        padding: 10px !important;
    }

    .card2 {
        padding: 50px;
        display: none;
    }

    .right {
        margin-top: 50px !important;
        margin-bottom: 50px !important;
    }

    .heading{
        display: none;
    }
}
</style>