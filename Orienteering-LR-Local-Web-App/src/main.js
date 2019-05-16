import Vue from "vue";
import App from "./App.vue";
import BoostrapVue from "bootstrap-vue";
import VueNativeSock from "vue-native-websocket";

import "bootstrap/dist/css/bootstrap.css";
import "bootstrap-vue/dist/bootstrap-vue.css";

Vue.config.productionTip = true;

Vue.use(BoostrapVue);
Vue.use(VueNativeSock, "ws://localhost:9696/leaderboard", { format: "json" });

new Vue({
    render: h => h(App)
}).$mount("#app");
