import Vue from "vue";
import App from "./App.vue";
import Control from "./components/Control.vue";
import Results from "./components/Results.vue"
import Leaderboard from "./components/Leaderboard.vue";
import BoostrapVue from "bootstrap-vue";
import App from "./App.vue";
import VueNativeSock from "vue-native-websocket";
import { library } from '@fortawesome/fontawesome-svg-core';
import { faBroadcastTower, faRunning, faEllipsisH } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import MarqueeText from 'vue-marquee-text-component'
import { app_store } from "./socket/store";
import router from "@/router";
import { library } from '@fortawesome/fontawesome-svg-core'
import { faBroadcastTower, faRunning, faEllipsisH } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import MarqueeText from 'vue-marquee-text-component'
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

library.add(faBroadcastTower, faRunning, faEllipsisH)

Vue.component('font-awesome-icon', FontAwesomeIcon)

Vue.component('marquee-text', MarqueeText)

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

new Vue({
  store: app_store,
  router,
  render (h) {
    return h(App);
  }
}).$mount("#app");
