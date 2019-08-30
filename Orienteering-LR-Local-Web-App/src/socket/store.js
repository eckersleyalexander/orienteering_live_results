import Vue from 'vue'
import Vuex from 'vuex'

import {
  SOCKET_ONOPEN,
  SOCKET_ONCLOSE,
  SOCKET_ONERROR,
  SOCKET_ONMESSAGE,
  SOCKET_RECONNECT,
  SOCKET_RECONNECT_ERROR
} from './mutation-types'

Vue.use(Vuex);

function makeAction(namespace, action, uuid, message) {
  return {
    namespace,
    action,
    uuid,
    payload: message
  }
}

function uuidv4() {
  return ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c =>
    (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
  )
}
const socket_uuid = uuidv4();

export const control = {
  namespaced: true,
  state: {
    socket: {
      online: false,
      message: '',
      reconnectError: false,
      clients: [],
      messages: [],
      classes: []
    }
  },
  mutations: {


    handleClientsMessage(state, message) {
      state.socket.clients = JSON.parse(message["payload"]);
    },

    handleClassesMessage(state, message) {
      state.socket.classes = JSON.parse(message["payload"]);
    }
  },
  actions: {
    clients(context, message) { context.commit("handleClientsMessage", message) },
    classes(context, message) { context.commit("handleClassesMessage", message) },
    error(context, message) { console.error(message) },
    // local actions (not called by socket messages) are prefixed with _
    _setLeaderboardClass(context, data) {
      Vue.prototype.$socket.sendObj(makeAction("control","setLeaderboardClass", data.uuid, data.raceClass))
    }
  }
}

export const leaderboard = {
  namespaced: true,
  state: {
    socket: {
      online: false,
      message: '',
      reconnectError: false,
    },
    leaderboard: {
      raceClass: null,
      data: []
    }
  },
  mutations: {
    handleSetLeaderboardMessage(state, message) {
      state.leaderboard.raceClass = message.payload;
    },
    handleLeaderboardUpdate(state,message) {
      console.log("leaderboard update");
      state.leaderboard.data = JSON.parse(message.payload);
    }
  },
  actions: {
    setLeaderboardClass(context, message) {context.commit("handleSetLeaderboardMessage", message);},
    leaderboardUpdate(context, message) {context.commit("handleLeaderboardUpdate", message);},
    error(context, message) { console.error(message) }
  }
}

const socketEvents = {
  [SOCKET_ONOPEN](state, event) {
    Vue.prototype.$socket = event.currentTarget
    state.control.socket.online = true
    console.log("socket connected")
    const namespace = window.location.pathname === "/control" ? "control" : "leaderboard";
    console.log("namespace is", namespace);
    Vue.prototype.$socket.sendObj(makeAction(namespace, "register", socket_uuid, null))
    Vue.prototype.$socket.sendObj(makeAction(namespace, "classes", socket_uuid, null))
  },
  [SOCKET_ONCLOSE](state, event) {
    state.control.socket.online = false
  },
  [SOCKET_ONERROR](state, event) {
    console.error(state, event)
  },
  // default handler called for all methods
  [SOCKET_ONMESSAGE](state, message) {
    state.socket.message = message
    console.error("Unhandled message:", message)
  },
  // mutations for reconnect methods
  [SOCKET_RECONNECT](state, count) {
    console.info(state, count)
  },
  [SOCKET_RECONNECT_ERROR](state) {
    state.socket.reconnectError = true;
  }
}

export const app_store = new Vuex.Store({
  modules: {
    control,
    leaderboard,
  },
  mutations: socketEvents
})