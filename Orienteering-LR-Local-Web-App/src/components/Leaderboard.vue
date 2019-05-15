<template>
    <div class="leaderboard">
        <div v-if="loading">Loading...</div>
        <div v-else>
            <table v-if="data">
                <thead>
                    <tr>
                        <th>Position</th>
                        <th>Name</th>
                        <!-- Need to have as many headers as radio controls -->
                        <th v-for="(item, index) in data[0].Times" :key="index">
                           Leg {{ index + 1 }}
                        </th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="(item, index) in data" :key="index">
                        <td>{{ index + 1 }}</td>
                        <td>{{ item.FirstName }} {{ item.LastName }}</td>
                        <td v-for="(time, index) in item.Times" :key="index">
                            {{ new Date(time).toISOString().slice(11, -2) }}
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</template>

<script>
export default {
    name: "Leaderboard",
    data() {
        return {
            loading: true,
            data: null
        };
    },
    created() {
        this.$options.sockets.onmessage = data => this.onmessage(data);
    },
    methods: {
        onmessage: function(msg) {
            this.data = JSON.parse(msg.data);
            this.loading = false;
        }
    }
};
</script>
