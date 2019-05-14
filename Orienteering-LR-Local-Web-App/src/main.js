import Vue from "vue";
import App from "./App.vue";

import VueSocketIO from "vue-socket.io";

Vue.config.productionTip = true;

Vue.use(VueSocketIO, "http://localhost:9696/leaderboard");

new Vue({
    render: h => h(App)
}).$mount("#app");
