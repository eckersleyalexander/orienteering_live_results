import Vue from "vue";
import App from "./App.vue";

Vue.config.productionTip = true;

import VueNativeSock from "vue-native-websocket";
Vue.use(VueNativeSock, "ws://localhost:9696/leaderboard", { format: "json" });

new Vue({
    render: h => h(App)
}).$mount("#app");
