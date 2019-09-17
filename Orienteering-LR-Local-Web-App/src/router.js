import Vue from "vue"
import Router from "vue-router"
import Leaderboard from "@/components/Leaderboard.vue";
import Control from "@/components/Control.vue";

Vue.use(Router)

export default new Router({
    routes: [
        {
            path: "/",
            name: "Leaderboard",
            component: Leaderboard
        },
        {
            path: "/control/",
            name: "control",
            component: Control
        }
    ]
})