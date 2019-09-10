import Vue from "vue";
import App from "./App.vue";
import Control from "./components/Control.vue";
import Results from "./components/Results.vue"
import Leaderboard from "./components/Leaderboard.vue";
import BoostrapVue from "bootstrap-vue";
import VueNativeSock from "vue-native-websocket";
import { library } from '@fortawesome/fontawesome-svg-core';
import { faBroadcastTower, faRunning, faEllipsisH } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import MarqueeText from 'vue-marquee-text-component' 
import { app_store } from "./socket/store";
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

library.add(faBroadcastTower, faRunning, faEllipsisH);


Vue.component('font-awesome-icon', FontAwesomeIcon);
Vue.component('marquee-text', MarqueeText);


const mutations = {
  SOCKET_ONOPEN,
  SOCKET_ONCLOSE,
  SOCKET_ONERROR,
  SOCKET_ONMESSAGE,
  SOCKET_RECONNECT,
  SOCKET_RECONNECT_ERROR
};

Vue.use(VueNativeSock, "ws://10.0.0.3:9696/socket", {
  format: "json",
  store: app_store,
  mutations: mutations
});

const routes = {
  "/": Leaderboard,
  "/control": Control,
  "/results": Results
};
const NotFound = { template: "<p>Page not found</p>" };
new Vue({
  data: {
    currentRoute: window.location.pathname
  },
  computed: {
    ViewComponent() {
      return routes[this.currentRoute] || NotFound;
    }
  },
  store: app_store,
  render(h) {
    return h(this.ViewComponent);
  }
}).$mount("#app");
