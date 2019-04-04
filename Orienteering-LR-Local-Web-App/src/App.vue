<template>
    <div id="app">
        <Home/>
        <ApiTest query="hello" />
    </div>
</template>

<script>
    import Home from './components/Home.vue';
    import ApiTest from './components/ApiTest.vue';

    export default {
        name: 'app',
        components: {
            Home,
            ApiTest
        }
    };
</script>

<style>
</style>
