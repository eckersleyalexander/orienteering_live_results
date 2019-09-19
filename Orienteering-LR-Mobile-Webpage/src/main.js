import Vue from "vue";
import App from "./App.vue";

import BootstrapVue from "bootstrap-vue";
import "bootstrap/dist/css/bootstrap.css";
import "bootstrap-vue/dist/bootstrap-vue.css";

import { library } from "@fortawesome/fontawesome-svg-core";
import {
  faBroadcastTower,
  faRunning,
  faEllipsisH,
  faSearch
} from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";

import VueAutosuggest from "vue-autosuggest";

Vue.use(VueAutosuggest);

Vue.use(BootstrapVue);

library.add(faBroadcastTower, faRunning, faEllipsisH, faSearch);

Vue.component("font-awesome-icon", FontAwesomeIcon);

Vue.config.productionTip = false;

new Vue({
  render: h => h(App)
}).$mount("#app");
