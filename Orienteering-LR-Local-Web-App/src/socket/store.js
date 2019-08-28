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

export const control_store = new Vuex.Store({
  state: {
    socket: {
      online: false,
      message: '',
      reconnectError: false,
      clients: [],
      messages: []
    }
  },
  mutations:{
    [SOCKET_ONOPEN] (state, event)  {
      Vue.prototype.$control_socket = event.currentTarget
      state.socket.online = true
      console.log("socket connected")
      Vue.prototype.$control_socket.sendObj({action:"register", uuid:"controller1"})
    },
    [SOCKET_ONCLOSE] (state, event)  {
      state.socket.online = false
    },
    [SOCKET_ONERROR] (state, event)  {
      console.error(state, event)
    },
    // default handler called for all methods
    [SOCKET_ONMESSAGE] (state, message)  {
      state.socket.message = message
      console.error("Unhandled message:",message)
    },
    // mutations for reconnect methods
    [SOCKET_RECONNECT](state, count) {
      console.info(state, count)
    },
    [SOCKET_RECONNECT_ERROR](state) {
      state.socket.reconnectError = true;
    },

    handleClientsMessage (state, message) {
      state.socket.clients = JSON.parse(message["payload"]);
    }
  },
  actions: {
    clients (context, message) { context.commit("handleClientsMessage", message) },
    error(context,message) {console.error(message)}
  }
})

export const leaderboard_store = new Vuex.Store({
  state: {
    socket: {
      online: false,
      message: '',
      reconnectError: false,
      clients: [],
      messages: []
    }
  },
  mutations:{
    [SOCKET_ONOPEN] (state, event)  {
      Vue.prototype.$leaderboard_socket = event.currentTarget
      state.socket.online = true
      console.log("socket connected")
    },
    [SOCKET_ONCLOSE] (state, event)  {
      state.socket.online = false
      console.info("socket disconnected")
    },
    [SOCKET_ONERROR] (state, event)  {
      console.error(state, event)
    },
    // default handler called for all methods
    [SOCKET_ONMESSAGE] (state, message)  {
      state.socket.message = message
      console.error("Unhandled message")
    },
    // mutations for reconnect methods
    [SOCKET_RECONNECT](state, count) {
      console.info(state, count)
    },
    [SOCKET_RECONNECT_ERROR](state) {
      state.socket.reconnectError = true;
    },

    handleClientsMessage (state, message) {
      state.socket.clients = message;
    }
  },
  actions: {
    error(context,message) {console.error(message)}
  }
})

export const app_store = new Vuex.Store({
  modules: {
    control: control_store,
    leaderboard: leaderboard_store
  }
})