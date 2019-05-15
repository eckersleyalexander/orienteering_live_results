<template>
    <div v-if="loading">Loading... {{ msg }}</div>
    <div v-else>{{ msg }}</div>
</template>

<script>
export default {
    name: "Leaderboard",
    data() {
        return {
            loading: true,
            msg: ""
        };
    },
    created() {
        this.$options.sockets.onmessage = data => this.onmessage(data);
    },
    methods: {
        onmessage: function(msg) {
            console.log(
                "received msg. initial values: " +
                    this.loading +
                    ", " +
                    this.msg
            );
            this.msg = msg.data;
            this.loading = false;
            console.log("new values: " + this.loading + ", " + this.msg);
        }
    }
};
</script>
