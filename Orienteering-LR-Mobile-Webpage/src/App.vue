<template>
  <div id="app">
    <Leaderboard />
  </div>
</template>

<script>
import Leaderboard from "./components/Leaderboard.vue";

export default {
  name: "app",
  components: {
    Leaderboard
  }
};
</script>

<style>
#app {
  font-family: "Avenir", Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  height: 100%;
  width: 100%;
}
</style>
