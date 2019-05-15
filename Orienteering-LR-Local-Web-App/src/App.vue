<template>
    <div id="app">
        <Home/>
        <!-- <ApiTest query="hello" /> -->
        <Leaderboard/>
    </div>
</template>

<script>
    import Home from './components/Home.vue';
    // import ApiTest from './components/ApiTest.vue';
    import Leaderboard from './components/Leaderboard.vue';

    export default {
        name: 'app',
        components: {
            Home,
            // ApiTest,
            Leaderboard
        },
        mounted () {
        }
    };
</script>

<style>
</style>
