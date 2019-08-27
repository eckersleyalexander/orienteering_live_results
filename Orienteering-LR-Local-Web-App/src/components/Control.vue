<template>
    <div class="control">
        <h1>Control panel</h1>
        <ul>
            <li v-for="client in this.$store.state.control.socket.clients" :key="client">
                {{client.id}}: {{client.type}}
            </li>
        </ul>
    </div>
</template>

<script>
export default {
    name: "Control",
    data() {
        return {
            loading: true,
        };
    },
    created() {
        // store.actions.broadcast = data => this.onmessage(data);
    },
    methods: {
        
    }
};
</script>
