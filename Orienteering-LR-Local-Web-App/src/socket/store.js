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
    _updateLeaderboard(context, data) {
      Vue.prototype.$socket.sendObj(makeAction("control","updateLeaderboard", data.uuid, data.raceClass))
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
      clients: [],
      messages: []
    }
  },
  mutations: {
    handleClientsMessage(state, message) {
      state.socket.clients = message;
    }
  },
  actions: {
    error(context, message) { console.error(message) }
  }
}

const socketEvents = {
  [SOCKET_ONOPEN](state, event) {
    Vue.prototype.$socket = event.currentTarget
    state.control.socket.online = true
    console.log("socket connected")
    Vue.prototype.$socket.sendObj(makeAction("control","register", "controller1", null))
    Vue.prototype.$socket.sendObj(makeAction("control","classes", "controller1", null))
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