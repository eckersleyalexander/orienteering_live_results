import Vue from "vue";
import App from "./App.vue";
import Control from "./components/Control.vue";
import BoostrapVue from "bootstrap-vue";
import VueNativeSock from "vue-native-websocket";
import {app_store} from "./socket/store";
import {
    SOCKET_ONOPEN,
    SOCKET_ONCLOSE,
    SOCKET_ONERROR,
    SOCKET_ONMESSAGE,
    SOCKET_RECONNECT,
    SOCKET_RECONNECT_ERROR
  } from "./socket/mutation-types";


import "bootstrap/dist/css/bootstrap.css";
import "bootstrap-vue/dist/bootstrap-vue.css";

Vue.config.productionTip = true;

Vue.use(BoostrapVue);

const mutations = {
    SOCKET_ONOPEN,
    SOCKET_ONCLOSE,
    SOCKET_ONERROR,
    SOCKET_ONMESSAGE,
    SOCKET_RECONNECT,
    SOCKET_RECONNECT_ERROR
  }
// Vue.use(VueNativeSock, "ws://localhost:9696/leaderboard", {
//      format: "json",
//      store: leaderboard_store,
//      mutations: mutations  
//     });

Vue.use(VueNativeSock, "ws://localhost:9696/control", {
    format: "json",
    store: app_store,
    mutations: mutations
    });

const routes = {
    "/": App,
    "/control": Control
}
const NotFound = { template: '<p>Page not found</p>' }

new Vue({
    data: {
        currentRoute: window.location.pathname
    },
    computed: {
        ViewComponent() {
            return routes[this.currentRoute] || NotFound
        }
    },
    store: app_store,
    render (h) {return h(this.ViewComponent)}
}).$mount("#app");
